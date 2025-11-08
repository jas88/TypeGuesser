using System;

namespace TypeGuesser;

/// <summary>
/// Immutable struct representing the result of type guessing operations.
/// This is a value type to enable zero-allocation returns from pooled builders.
/// </summary>
/// <remarks>
/// This struct provides all the information needed to create a database column
/// that can accommodate the analyzed data, including type, precision, scale,
/// width requirements, and unicode support.
/// </remarks>
public readonly struct TypeGuessResult
{
    /// <summary>
    /// The inferred C# type that best represents all analyzed values.
    /// </summary>
    public Type CSharpType { get; init; }

    /// <summary>
    /// The maximum width in characters needed to represent the data as strings.
    /// Relevant for varchar/nvarchar column sizing.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// The number of digits required before the decimal point.
    /// For integers, this is the number of digits. For decimals, this is precision - scale.
    /// </summary>
    public int DigitsBeforeDecimal { get; init; }

    /// <summary>
    /// The number of digits required after the decimal point (i.e., the scale).
    /// Zero for integer types.
    /// </summary>
    public int DigitsAfterDecimal { get; init; }

    /// <summary>
    /// Indicates whether the data contains non-ASCII characters requiring unicode storage (nvarchar vs varchar).
    /// </summary>
    public bool RequiresUnicode { get; init; }

    /// <summary>
    /// The total number of non-null values processed during type guessing.
    /// </summary>
    public int ValueCount { get; init; }

    /// <summary>
    /// The total number of null or DBNull values encountered during type guessing.
    /// </summary>
    public int NullCount { get; init; }

    /// <summary>
    /// Gets the total number of significant digits (precision for SQL decimal types).
    /// This is the sum of <see cref="DigitsBeforeDecimal"/> and <see cref="DigitsAfterDecimal"/>.
    /// </summary>
    public int DecimalPrecision => DigitsBeforeDecimal + DigitsAfterDecimal;

    /// <summary>
    /// Gets the number of digits after the decimal point (scale for SQL decimal types).
    /// This is an alias for <see cref="DigitsAfterDecimal"/>.
    /// </summary>
    public int DecimalScale => DigitsAfterDecimal;

    /// <summary>
    /// Returns true if no decimal places are required (i.e., <see cref="DecimalPrecision"/> is zero).
    /// </summary>
    public bool IsDecimalSizeEmpty => DecimalPrecision == 0;

    /// <summary>
    /// Creates a new <see cref="TypeGuessResult"/> with the specified properties.
    /// </summary>
    /// <param name="cSharpType">The inferred C# type</param>
    /// <param name="width">Maximum string width in characters</param>
    /// <param name="digitsBeforeDecimal">Digits before decimal point</param>
    /// <param name="digitsAfterDecimal">Digits after decimal point</param>
    /// <param name="requiresUnicode">Whether unicode support is needed</param>
    /// <param name="valueCount">Number of non-null values processed</param>
    /// <param name="nullCount">Number of null values encountered</param>
    public TypeGuessResult(
        Type cSharpType,
        int? width = null,
        int digitsBeforeDecimal = 0,
        int digitsAfterDecimal = 0,
        bool requiresUnicode = false,
        int valueCount = 0,
        int nullCount = 0)
    {
        CSharpType = cSharpType ?? typeof(string);
        Width = width;
        DigitsBeforeDecimal = Math.Max(0, digitsBeforeDecimal);
        DigitsAfterDecimal = Math.Max(0, digitsAfterDecimal);
        RequiresUnicode = requiresUnicode;
        ValueCount = valueCount;
        NullCount = nullCount;
    }

    /// <summary>
    /// Converts this result to a <see cref="DatabaseTypeRequest"/> for backward compatibility.
    /// </summary>
    /// <returns>A new <see cref="DatabaseTypeRequest"/> instance with equivalent settings</returns>
    public DatabaseTypeRequest ToDatabaseTypeRequest()
    {
        var decimalSize = new DecimalSize(DigitsBeforeDecimal, DigitsAfterDecimal);
        return new DatabaseTypeRequest(CSharpType, Width, decimalSize)
        {
            Unicode = RequiresUnicode
        };
    }

    /// <summary>
    /// Creates a <see cref="TypeGuessResult"/> from an existing <see cref="DatabaseTypeRequest"/>.
    /// </summary>
    /// <param name="request">The database type request to convert</param>
    /// <param name="valueCount">Number of values that were processed</param>
    /// <param name="nullCount">Number of null values encountered</param>
    /// <returns>A new <see cref="TypeGuessResult"/> with equivalent type information</returns>
    public static TypeGuessResult FromDatabaseTypeRequest(
        DatabaseTypeRequest request,
        int valueCount = 0,
        int nullCount = 0)
    {
        return new TypeGuessResult(
            request.CSharpType,
            request.Width,
            request.Size.NumbersBeforeDecimalPlace,
            request.Size.NumbersAfterDecimalPlace,
            request.Unicode,
            valueCount,
            nullCount);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{CSharpType.Name}" +
               (Width.HasValue ? $"({Width})" : string.Empty) +
               (DecimalPrecision > 0 ? $"({DecimalPrecision},{DecimalScale})" : string.Empty) +
               (RequiresUnicode ? " unicode" : string.Empty) +
               $" [{ValueCount} values, {NullCount} nulls]";
    }
}
