using System;
using System.Globalization;

namespace TypeGuesser.Deciders;

/// <summary>
/// Guesses whether strings are <see cref="int"/> and handles parsing approved strings according to the <see cref="DecideTypesForStrings{T}.Culture"/>
/// </summary>
/// <remarks>
/// Creates a new instance for recognizing whole numbers in string values
/// </remarks>
/// <param name="culture"></param>
public sealed class IntTypeDecider(CultureInfo culture) : DecideTypesForStrings<int>(culture,TypeCompatibilityGroup.Numerical, typeof(byte), typeof(short), typeof(int))
{
    /// <inheritdoc/>
    protected override IDecideTypesForStrings CloneCore(CultureInfo culture)
    {
        return new IntTypeDecider(culture);
    }

    /// <inheritdoc />
    protected override object ParseCore(ReadOnlySpan<char> value) => int.Parse(value, NumberStyles.Any, Culture.NumberFormat);

    /// <inheritdoc/>
    protected override bool IsAcceptableAsTypeCore(ReadOnlySpan<char> candidateString, IDataTypeSize? size)
    {
        if(IsExplicitDate(candidateString))
            return false;

        if (!int.TryParse(candidateString, NumberStyles.Any, Culture.NumberFormat, out var i)) return false;

        size?.Size.IncreaseTo(i.ToString(CultureInfo.InvariantCulture).Trim('-').Length,0);
        return true;
    }
}
