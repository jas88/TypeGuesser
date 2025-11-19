using System;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Linq;

namespace TypeGuesser;

/// <summary>
/// Describes a cross platform database field type you want created including maximum width for string based columns and precision/scale for
/// decimals.
/// </summary>
public class DatabaseTypeRequest : IDataTypeSize
{
    private static readonly Type[] _preferenceOrderArray =
    [
        typeof(bool),
        typeof(int),
        typeof(decimal),
        typeof(long),     // SQL bigint

        typeof(TimeSpan),
        typeof(DateTime), //ironically Convert.ToDateTime likes int and floats as valid dates -- nuts

        typeof(byte),     // SQL tinyint - less common, more specific
        typeof(short),    // SQL smallint - less common, more specific
        typeof(Guid),     // SQL uniqueidentifier - never guessed from strings
        typeof(byte[]),   // SQL varbinary - never guessed from strings

        typeof(string)
    ];

    /// <summary>
    /// Any input string of unknown Type will be assignable to one of the following C# data types.  The order denotes system wide which
    /// data types to try  converting the string into in order of preference.  For the implementation of this see <see cref="Guesser"/>.
    /// </summary>
    public static readonly ReadOnlyCollection<Type> PreferenceOrder = new(_preferenceOrderArray);

    /// <summary>
    /// Fast O(1) lookup for type index in PreferenceOrder.
    /// </summary>
    public static readonly FrozenDictionary<Type, int> PreferenceOrderIndex =
        _preferenceOrderArray.Select((type, index) => new { type, index })
            .ToFrozenDictionary(x => x.type, x => x.index);

    private int? _maxWidthForStrings;

    /// <summary>
    /// The <see cref="System.Type"/> which this metadata describes.
    /// </summary>
    public Type CSharpType { get; set; }

    /// <summary>
    /// The <see cref="DecimalSize"/> of the largest scale / precision you want to be able to represent.  This is valid even if <see cref="CSharpType"/>
    /// is not a decimal (e.g. int).
    /// </summary>
    public DecimalSize Size { get; set; }

    /// <summary>
    /// The width in characters of the longest string representation of data you want to support.  This is valid even if <see cref="CSharpType"/>
    /// is not a string (E.g. a decimal).
    /// </summary>
    public int? Width
    {
        get
        {
            // If explicit width is set, compare with size length
            if (_maxWidthForStrings.HasValue)
            {
                var sizeLength = Size.ToStringLength();
                return sizeLength > 0 ? Math.Max(_maxWidthForStrings.Value, sizeLength) : _maxWidthForStrings.Value;
            }

            // No explicit width - return size length only if non-zero AND type is numeric
            if (CSharpType == typeof(decimal) || CSharpType == typeof(int))
            {
                var sizeLengthOnly = Size.ToStringLength();
                return sizeLengthOnly > 0 ? sizeLengthOnly : null;
            }

            return null;
        }
        set => _maxWidthForStrings = value;
    }

    /// <summary>
    /// Only applies when <see cref="CSharpType"/> is <see cref="string"/>.  True indicates that the column should be
    /// nvarchar instead of varchar.
    /// </summary>
    public bool Unicode { get; set; }

    /// <summary>
    /// Creates a new instance with the given initial type description
    /// </summary>
    /// <param name="cSharpType"></param>
    /// <param name="maxWidthForStrings"></param>
    /// <param name="decimalPlacesBeforeAndAfter"></param>
    public DatabaseTypeRequest(Type cSharpType, int? maxWidthForStrings = null,
        DecimalSize? decimalPlacesBeforeAndAfter = null)
    {
        CSharpType = cSharpType;
        Width = maxWidthForStrings;
        Size = decimalPlacesBeforeAndAfter ?? new DecimalSize();
    }

    /// <summary>
    /// Creates a new instance requesting a string type
    /// </summary>
    public DatabaseTypeRequest() : this(typeof(string))
    {
    }

    #region Equality
    /// <summary>
    /// Property based equality. Compares only the properties relevant to each type:
    /// - string: CSharpType, _maxWidthForStrings, Unicode
    /// - decimal: CSharpType, Size (precision/scale)
    /// - byte[]: CSharpType, _maxWidthForStrings
    /// - All other types (bool, int, long, DateTime, TimeSpan, Guid, etc.): CSharpType only
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    private bool Equals(DatabaseTypeRequest other)
    {
        if (CSharpType != other.CSharpType) return false;

        var underlyingType = Nullable.GetUnderlyingType(CSharpType) ?? CSharpType;

        // String: Compare _maxWidthForStrings and Unicode (Size is irrelevant for varchar/nvarchar)
        // Note: We compare the backing field, not the Width property, because Width includes Size.ToStringLength()
        if (underlyingType == typeof(string))
        {
            return _maxWidthForStrings == other._maxWidthForStrings && Unicode == other.Unicode;
        }

        // Decimal: Compare Size only (Width/Unicode are irrelevant for decimal(p,s))
        if (underlyingType == typeof(decimal))
        {
            return Equals(Size, other.Size);
        }

        // byte[]: Compare _maxWidthForStrings only (Unicode/Size are irrelevant for varbinary(n))
        // Note: We compare the backing field, not the Width property, because Width includes Size.ToStringLength()
        if (underlyingType == typeof(byte[]))
        {
            return _maxWidthForStrings == other._maxWidthForStrings;
        }

        // All other types (bool, byte, short, int, long, float, double, DateTime, TimeSpan, Guid):
        // Type alone is sufficient - these have fixed SQL storage
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((DatabaseTypeRequest)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var underlyingType = Nullable.GetUnderlyingType(CSharpType) ?? CSharpType;

        // Hash code must match Equals() logic
        if (underlyingType == typeof(string))
        {
            return HashCode.Combine(CSharpType, _maxWidthForStrings, Unicode);
        }

        if (underlyingType == typeof(decimal))
        {
            return HashCode.Combine(CSharpType, Size);
        }

        if (underlyingType == typeof(byte[]))
        {
            return HashCode.Combine(CSharpType, _maxWidthForStrings);
        }

        // For all other types, only hash the type
        return CSharpType.GetHashCode();
    }

    /// <summary>
    /// Overridden operator for <see cref="Equals(TypeGuesser.DatabaseTypeRequest)"/>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(DatabaseTypeRequest left, DatabaseTypeRequest right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Overridden operator for <see cref="Equals(TypeGuesser.DatabaseTypeRequest)"/>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(DatabaseTypeRequest left, DatabaseTypeRequest right)
    {
        return !Equals(left, right);
    }
    #endregion

    /// <summary>
    /// Returns a <see cref="DatabaseTypeRequest"/> in which the <see cref="Width"/> etc are large enough to accommodate
    /// both <paramref name="first" /> and <paramref name="second" />.  This may be a new instance or it may be <paramref name="first" />
    /// or <paramref name="second" /> (if one is already big enough to encompass the other).
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static DatabaseTypeRequest Max(DatabaseTypeRequest first, DatabaseTypeRequest second)
    {
        // Validate both types are supported
        var firstIndex = PreferenceOrder.IndexOf(first.CSharpType);
        var secondIndex = PreferenceOrder.IndexOf(second.CSharpType);

        if (firstIndex == -1 || secondIndex == -1)
        {
            throw new NotSupportedException(ErrorFormatters.CannotCombineTypes(first.CSharpType, second.CSharpType));
        }

        // If types differ, return the one with LOWER index (higher preference)
        // Lower index = earlier in preference order = more restrictive = higher preference
        if (firstIndex < secondIndex)
        {
            first.Unicode = first.Unicode || second.Unicode;
            return first;
        }

        if (firstIndex > secondIndex)
        {
            second.Unicode = first.Unicode || second.Unicode;
            return second;
        }

        // Types are the same, check if one instance is already large enough
        var newMaxWidthIfStrings = first.Width;

        //if first doesn't have a max string width
        if (newMaxWidthIfStrings == null)
            newMaxWidthIfStrings = second.Width; //use the second
        else if (second.Width != null)
            newMaxWidthIfStrings = Math.Max(newMaxWidthIfStrings.Value, second.Width.Value); //else use the max of the two

        var combinedSize = DecimalSize.Combine(first.Size, second.Size);
        var combinedUnicode = first.Unicode || second.Unicode;

        // Check if first is already large enough
        if (first.Width.GetValueOrDefault() >= newMaxWidthIfStrings.GetValueOrDefault() &&
            first.Size.NumbersBeforeDecimalPlace >= combinedSize.NumbersBeforeDecimalPlace &&
            first.Size.NumbersAfterDecimalPlace >= combinedSize.NumbersAfterDecimalPlace &&
            first.Unicode == combinedUnicode)
        {
            return first;
        }

        // Check if second is already large enough
        if (second.Width.GetValueOrDefault() >= newMaxWidthIfStrings.GetValueOrDefault() &&
            second.Size.NumbersBeforeDecimalPlace >= combinedSize.NumbersBeforeDecimalPlace &&
            second.Size.NumbersAfterDecimalPlace >= combinedSize.NumbersAfterDecimalPlace &&
            second.Unicode == combinedUnicode)
        {
            return second;
        }

        //types are the same, need to create new combined instance
        return new DatabaseTypeRequest(
                first.CSharpType,
                newMaxWidthIfStrings,
                combinedSize
            )
        { Unicode = combinedUnicode };
    }
}