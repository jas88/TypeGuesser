using System;

namespace TypeGuesser;

/// <summary>
/// Controls guess decisions where the choice is ambiguous e.g. is "T" and "F" True / False or just a string.  Use
/// <see cref="GuessSettingsFactory"/> to create instances
/// </summary>
public class GuessSettings
{
    /// <summary>
    /// True if single letter characters (e.g. "T"/"F" or "J/N" or "Y"/"N") should be interpreted as True/False
    /// </summary>
    public bool CharCanBeBoolean { get; set; } = true;


    /// <summary>
    /// Optional, when set dates must be in one of these formats and any string in this format will be picked as a date.
    /// </summary>
    public string[]? ExplicitDateFormats { get; set; } = null;


    /// <summary>
    /// Creates a shallow clone of the settings
    /// </summary>
    /// <returns></returns>
    public GuessSettings Clone()
    {
        return (GuessSettings)MemberwiseClone();
    }

    /// <summary>
    /// Copies all values of this object into <paramref name="copyInto"/>
    /// </summary>
    /// <param name="copyInto">The instance to populate with the current values of this</param>
    public void CopyTo(GuessSettings copyInto)
    {
        copyInto.CharCanBeBoolean = CharCanBeBoolean;
        copyInto.ExplicitDateFormats = ExplicitDateFormats;
    }

    /// <summary>
    /// Creates a new instance with default values
    /// </summary>
    public GuessSettings()
    {
        CharCanBeBoolean = true;
        ExplicitDateFormats = null;
    }

    /// <summary>
    /// Value-based equality comparison
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not GuessSettings other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (CharCanBeBoolean != other.CharCanBeBoolean)
            return false;

        // Compare ExplicitDateFormats arrays
        if (ExplicitDateFormats == null && other.ExplicitDateFormats == null)
            return true;

        if (ExplicitDateFormats == null || other.ExplicitDateFormats == null)
            return false;

        if (ExplicitDateFormats.Length != other.ExplicitDateFormats.Length)
            return false;

        for (int i = 0; i < ExplicitDateFormats.Length; i++)
        {
            if (ExplicitDateFormats[i] != other.ExplicitDateFormats[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Value-based hash code
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CharCanBeBoolean);

        if (ExplicitDateFormats != null)
        {
            foreach (var format in ExplicitDateFormats)
            {
                hash.Add(format);
            }
        }

        return hash.ToHashCode();
    }
}