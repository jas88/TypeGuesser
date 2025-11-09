using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace TypeGuesser;

/// <summary>
/// Calculates a <see cref="DatabaseTypeRequest"/> based on a collection of objects seen so far.  This allows you to take a DataTable column (which might be only string
/// formatted) and identify an appropriate database type to hold the data.  For example if you see "2001-01-01" in the first row of column then the database
/// type can be 'datetime' but if you subsequently see 'n\a' then it must become 'varchar(10)' (since 2001-01-01 is 10 characters long).
///
/// <para>Includes support for DateTime, Timespan, String (including calculating max length), Int, Decimal (including calculating scale/precision). </para>
///
/// <para><see cref="Guesser"/> will always use the most restrictive data type possible first and then fall back on weaker types as new values are seen that do not fit
/// the guessed Type, ultimately falling back to varchar(x).</para>
///
/// <para>Thread Safety (v2.0+): Each <see cref="Guesser"/> instance uses a pooled builder internally for improved performance.
/// Instances are not thread-safe and should not be shared between threads. For thread-safe scenarios, create separate instances per thread
/// or use the static <see cref="GetSharedFactory"/> method to obtain a thread-safe <see cref="TypeDeciderFactory"/>.</para>
///
/// <para>Performance (v2.0+): For optimal performance, prefer passing hard-typed values (int, decimal, bool) instead of strings
/// when possible. The internal pooled builder uses zero-allocation optimizations for these types.</para>
/// </summary>
public sealed class Guesser : IDisposable
{
    /// <summary>
    /// The pooled builder that performs the actual type guessing work.
    /// </summary>
    private readonly PooledBuilder _builder;

    /// <summary>
    /// Cached reference to the builder's internal state for backward compatibility.
    /// </summary>
    private DatabaseTypeRequest? _lastGuess;

    /// <summary>
    /// Flag to track if this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Backing field for ExtraLengthPerNonAsciiCharacter to support init-only setter.
    /// </summary>
    private int _extraLengthPerNonAsciiCharacter;

    /// <summary>
    /// Controls behaviour of deciders during <see cref="AdjustToCompensateForValue"/>
    /// </summary>
    public GuessSettings Settings => _builder.Settings;

    /// <summary>
    /// Normally when measuring the lengths of strings something like "It�s" would be 4 but for Oracle it needs extra width.  If this is
    /// non zero then when <see cref="AdjustToCompensateForValue(object)"/> is a string then any non standard characters will have this number
    /// added to the length predicted.
    /// </summary>
    public int ExtraLengthPerNonAsciiCharacter
    {
        get => _builder.ExtraLengthPerNonAsciiCharacter;
        init
        {
            _extraLengthPerNonAsciiCharacter = value;
            // _builder is set in constructor which runs before init setters, so we can update it directly
            if (_builder != null)
            {
                _builder.ExtraLengthPerNonAsciiCharacter = value;
            }
        }
    }

    /// <summary>
    /// The minimum amount of characters required to represent date values stored in the database when issuing ALTER statement to convert
    /// the column to allow strings.
    /// </summary>
    public const int MinimumLengthRequiredForDateStringRepresentation = 27;

    /// <summary>
    /// The currently computed data type (including string length / decimal scale/precisione etc) that can store all values seen
    /// by <see cref="AdjustToCompensateForValue"/> so far.
    /// </summary>
    public DatabaseTypeRequest Guess
    {
        get
        {
            var result = _builder.Build();
            _lastGuess = ConvertToLegacyFormat(result);
            return _lastGuess;
        }
    }

    /// <summary>
    /// The culture to use for type deciders, determines what symbol decimal place is etc
    /// </summary>
    public CultureInfo Culture
    {
        set => _builder.SetCulture(value);
    }

    /// <summary>
    /// Becomes true when <see cref="AdjustToCompensateForValue"/> is called with a hard Typed object (e.g. int). This prevents a <see cref="Guesser"/>
    /// from being used with mixed Types of input (you should run only strings or only hard typed objects).
    /// </summary>
    public bool IsPrimedWithBonafideType
    {
        get
        {
            var result = _builder.Build();
            return result.ValueCount > 0 && result.CSharpType != typeof(string);
        }
    }

    /// <summary>
    /// Creates a new DataType
    /// </summary>
    public Guesser() : this(new DatabaseTypeRequest(DatabaseTypeRequest.PreferenceOrder[0]))
    {
    }

    /// <summary>
    /// Creates a new <see cref="Guesser"/> primed with the size of the given <paramref name="request"/>.
    /// </summary>
    /// <param name="request"></param>
    public Guesser(DatabaseTypeRequest request)
    {
        // Note: Object initializers run AFTER this line but BEFORE constructor body
        // So _extraLengthPerNonAsciiCharacter may already be set
        _builder = TypeGuesserBuilderPool.Rent(CultureInfo.CurrentCulture);

        // Apply the ExtraLengthPerNonAsciiCharacter setting to the builder
        // This is called in constructor body, which runs AFTER object initializers
        _builder.ExtraLengthPerNonAsciiCharacter = _extraLengthPerNonAsciiCharacter;

        ThrowIfNotSupported(request.CSharpType);

        // If the request specifies a non-default type, prime the builder
        if (request.CSharpType != DatabaseTypeRequest.PreferenceOrder[0])
        {
            // Prime the builder by processing a dummy value of the correct type
            // This maintains backward compatibility with the old constructor behavior
            PrimeBuilderWithType(request);
        }

        _lastGuess = request;
    }

    /// <summary>
    /// Gets a shared <see cref="TypeDeciderFactory"/> for the specified culture.
    /// This factory is cached and thread-safe, making it suitable for advanced scenarios
    /// where you need direct access to type deciders without creating a <see cref="Guesser"/> instance.
    /// </summary>
    /// <param name="culture">The culture to use for type parsing. If null, uses <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>A cached, thread-safe type decider factory</returns>
    public static TypeDeciderFactory GetSharedFactory(CultureInfo? culture = null)
    {
        return TypeGuesserBuilderPool.GetOrCreateDeciderFactory(culture ?? CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Disposes this <see cref="Guesser"/> instance and returns the pooled builder for reuse.
    /// </summary>
    /// <remarks>
    /// After calling Dispose, do not call any methods on this instance.
    /// It is safe to call Dispose multiple times; subsequent calls are no-ops.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        TypeGuesserBuilderPool.Return(_builder);
        _disposed = true;
    }

    /// <summary>
    /// Converts a <see cref="TypeGuessResult"/> struct to a <see cref="DatabaseTypeRequest"/> for backward compatibility.
    /// </summary>
    /// <param name="result">The type guess result to convert</param>
    /// <returns>A database type request with equivalent settings</returns>
    private static DatabaseTypeRequest ConvertToLegacyFormat(TypeGuessResult result)
    {
        return result.ToDatabaseTypeRequest();
    }

    /// <summary>
    /// Primes the builder with the type and size information from a DatabaseTypeRequest.
    /// </summary>
    /// <param name="request">The request to use for priming</param>
    private void PrimeBuilderWithType(DatabaseTypeRequest request)
    {
        // Set the initial type hint without processing values
        // This maintains backward compatibility where the constructor sets an initial type preference
        _builder.SetInitialTypeHint(request.CSharpType);
    }

    /// <summary>
    /// Runs <see cref="AdjustToCompensateForValue"/> on all cells in <see cref="DataRow"/> under the <paramref name="column"/>
    /// </summary>
    /// <param name="column"></param>
    public void AdjustToCompensateForValues(DataColumn column)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dt = column.Table;
        if (dt == null) return;

        foreach (DataRow row in dt.Rows)
            AdjustToCompensateForValue(row[column]);
    }

    /// <summary>
    /// Runs <see cref="AdjustToCompensateForValue"/> on all objects in the <paramref name="collection"/>
    /// </summary>
    /// <param name="collection"></param>
    public void AdjustToCompensateForValues(IEnumerable<object> collection)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var o in collection)
            AdjustToCompensateForValue(o);
    }

    /// <summary>
    /// <para>Adjusts the current <see cref="Guess"/> based on the <paramref name="o"/>.  All calls to this method for a given <see cref="Guesser"/>
    /// instance must be of the same Type e.g. string.  If you pass a hard Typed value in (e.g. int) then the <see cref="Guess"/> will change
    /// to the Type of the object but it will still calculate length/digits.
    /// </para>
    ///
    /// <para>Passing null / <see cref="DBNull.Value"/> is always allowed and never changes the <see cref="Guess"/></para>
    ///
    /// <para>Performance Tip (v2.0+): For best performance, pass hard-typed values (int, decimal, bool) instead of strings
    /// when possible. This enables zero-allocation optimizations in the internal pooled builder.</para>
    /// </summary>
    /// <exception cref="MixedTypingException">Thrown if you mix strings with hard Typed objects when supplying <paramref name="o"/></exception>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed</exception>
    /// <param name="o"></param>
    public void AdjustToCompensateForValue(object? o)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Delegate to the pooled builder which handles all the logic
        _builder.Process(o);

        // Clear cached guess so next access rebuilds from builder
        _lastGuess = null;
    }


    /// <summary>
    /// Returns true if the <see cref="Guess"/>  is considered to be an improvement on the DataColumn provided. Use only when you actually want to
    /// consider changing the value.  For example if you have read a CSV file into a DataTable and all current columns string/object then you can call this method
    /// to determine whether the <see cref="Guesser"/> found a more appropriate Type or not.
    ///
    /// <para>Note that if you want to change the Type you need to clone the DataTable, see: https://stackoverflow.com/questions/9028029/how-to-change-datatype-of-a-datacolumn-in-a-datatable</para>
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool ShouldDowngradeColumnTypeToMatchCurrentEstimate(DataColumn col)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        //it's not a string or an object, user probably has a type in mind for his DataColumn, let's not change that
        if (col.DataType != typeof(object) && col.DataType != typeof(string)) return false;

        var indexOfCurrentPreference = DatabaseTypeRequest.PreferenceOrder.IndexOf(Guess.CSharpType);
        var indexOfCurrentColumn = DatabaseTypeRequest.PreferenceOrder.IndexOf(typeof(string));

        //e.g. if current preference based on data is DateTime/integer and col is a string then we SHOULD downgrade
        return indexOfCurrentPreference < indexOfCurrentColumn;
    }

    private void ThrowIfNotSupported(Type currentEstimate)
    {
        if (currentEstimate == typeof(string))
            return;

        var factory = GetSharedFactory(_builder.Culture);
        if (!factory.IsSupported(currentEstimate))
            throw new NotSupportedException(ErrorFormatters.UnsupportedType(currentEstimate));
    }

    /// <summary>
    /// Parses the given <paramref name="val"/> into a hard typed object that matches the current <see cref="Guess"/>
    /// </summary>
    /// <param name="val"></param>
    /// <exception cref="NotSupportedException">If the current <see cref="Guess"/> does not have a parser defined</exception>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed</exception>
    /// <returns></returns>
    public object? Parse(string val)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var guessType = Guess.CSharpType;

        if (guessType == typeof(string))
            return val;

        ThrowIfNotSupported(guessType);

        var factory = GetSharedFactory(_builder.Culture);
        return factory.Dictionary[guessType].Parse(val);
    }
}