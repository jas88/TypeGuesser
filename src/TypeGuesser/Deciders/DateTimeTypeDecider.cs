using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TypeGuesser.Deciders;

/// <summary>
/// Guesses whether strings are <see cref="DateTime"/> and handles parsing approved strings according to the <see cref="Culture"/>
/// </summary>
public class DateTimeTypeDecider : DecideTypesForStrings<DateTime>
{
    private readonly TimeSpanTypeDecider _timeSpanTypeDecider;
    private readonly DecimalTypeDecider _decimalChecker;

    /// <summary>
    /// Array of all supported DateTime formats in which the Month appears before the Day e.g. e.g. "MMM-dd-yy" ("Sep-16-19")
    /// </summary>
    public static readonly string[] DateFormatsMD;

    /// <summary>
    /// Array of all supported DateTime formats in which the Day appears before the Month e.g. "dd-MMM-yy" ("16-Sep-19")
    /// </summary>
    public static readonly string[] DateFormatsDM;

    /// <summary>
    /// Array of all supported Time formats e.g. "h:mm:ss tt" ("9:34:39 AM")
    /// </summary>
    public static readonly string[] TimeFormats;

    private string[] _dateFormatToUse;
    private CultureInfo _culture;

    /// <summary>
    /// Creates a new instance for detecting/parsing <see cref="DateTime"/> strings according to the <paramref name="cultureInfo"/>
    /// </summary>
    /// <param name="cultureInfo">The culture to use for parsing. If null, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
    public DateTimeTypeDecider(CultureInfo cultureInfo) : base(cultureInfo ?? CultureInfo.CurrentCulture, TypeCompatibilityGroup.Exclusive, typeof(DateTime))
    {
        // Use CurrentCulture if null is passed
        cultureInfo ??= CultureInfo.CurrentCulture;

        _timeSpanTypeDecider = new(cultureInfo);
        _decimalChecker = new(cultureInfo);
        _dateFormatToUse = cultureInfo.DateTimeFormat.ShortDatePattern.IndexOf('M') > cultureInfo.DateTimeFormat.ShortDatePattern.IndexOf('d')
            ? DateFormatsDM
            : DateFormatsMD;
        _culture = cultureInfo;
    }

    /// <summary>
    /// Setting this to false will prevent <see cref="GuessDateFormat(IEnumerable{string})"/> changing the <see cref="Culture"/> e.g. when
    /// inserting date times
    /// </summary>
    public static bool AllowCultureGuessing { get; set; } = true;

    /// <summary>
    /// Explicitly sets the culture to use for processing date times.  This suppresses <see cref="GuessDateFormat(IEnumerable{string})"/>.
    /// Set to null to restore the current environment culture (and re-enable guessing).
    /// 
    /// </summary>
    public override CultureInfo Culture
    {
        get => _culture;
        set
        {
            _dateFormatToUse = value.DateTimeFormat.ShortDatePattern.IndexOf('M') > value.DateTimeFormat.ShortDatePattern.IndexOf('d') ? DateFormatsDM : DateFormatsMD;
            _culture = value;
        }
    }

    static DateTimeTypeDecider()
    {
        //all dates on their own
        var dateFormatsMd = (from y in YearFormats
                            from m in MonthFormats
                            from d in DayFormats
                            from dateSeparator in DateSeparators
                            from format in new[] {
                                string.Join(dateSeparator, m, d, y),
                                string.Join(dateSeparator, y, m, d)
                            }
                            select format).ToArray();

        var dateFormatsDm = (from y in YearFormats
                            from m in MonthFormats
                            from d in DayFormats
                            from dateSeparator in DateSeparators
                            from format in new[] {
                                string.Join(dateSeparator, d, m, y),
                                string.Join(dateSeparator, y, m, d)
                            }
                            select format).ToArray();

        //then all the times
        var timeFormats = (from timeSeparator in TimeSeparators
                          from suffix in Suffixes
                          from h in HourFormats
                          from m in MinuteFormats
                          from format in new[] {
                              string.Join(timeSeparator, h, m),
                              $"{string.Join(timeSeparator, h, m)} {suffix}"
                          }.Concat(
                              from s in SecondFormats
                              from fmt in new[] {
                                  string.Join(timeSeparator, h, m, s),
                                  $"{string.Join(timeSeparator, h, m, s)} {suffix}"
                              }
                              select fmt
                          )
                          select format).ToArray();

        DateFormatsDM = dateFormatsDm;
        DateFormatsMD = dateFormatsMd;
        TimeFormats = timeFormats;
    }

    private static readonly string[] YearFormats = [
        "yy",
        "yyy",
        "yyyy",
        "yyyyy"
    ];

    private static readonly string[] MonthFormats = [
        "M",
        "MM",
        "MMM",
        "MMMM"
    ];

    private static readonly string[] DayFormats = [
        "dd",
        "ddd",
        "dddd"
    ];

    private static readonly string[] DateSeparators = [
        "\\\\",
        "/",
        "-",
        "."
    ];

    private static readonly string[] HourFormats = [
        "h",
        "hh",
        "H",
        "HH"
    ];

    private static readonly string[] MinuteFormats = [
        "m",
        "mm"
    ];

    private static readonly string[] SecondFormats = [
        "s",
        "ss"
    ];

    private static readonly string[] Suffixes = [
        "tt"
    ];

    private static readonly string[] TimeSeparators = [
        ":"
    ];

    /// <inheritdoc/>
    protected override IDecideTypesForStrings CloneCore(CultureInfo culture)
    {
        return new DateTimeTypeDecider(culture);
    }

    /// <inheritdoc/>
    protected override object ParseCore(ReadOnlySpan<char> value)
    {
        // if user has specified a specific format that we are to use, use it
        if (Settings.ExplicitDateFormats != null)
            return DateTime.ParseExact(value, Settings.ExplicitDateFormats, _culture, DateTimeStyles.None);

        // otherwise parse a value using any of the valid culture formats
        if (!TryBruteParse(value, out var dt))
            throw new FormatException(ErrorFormatters.DateTimeParseError(new string(value)));

        return dt;
    }

    /// <summary>
    /// Makes guess about whether to use MD or DM based on the <paramref name="samples"/>.
    /// Where no samples, or no matches or the same number of matches DM is used.
    /// the samples.
    /// 
    /// <para>If <see cref="Culture"/> has been set then this method is ignored.  If the static property <see cref="AllowCultureGuessing"/>
    /// is set then it is also ignored.</para>
    /// </summary>
    public void GuessDateFormat(IEnumerable<string> samples)
    {
        var total = 0;
        var simple = 0;
        var m = 0;
        var d = 0;

        if (!AllowCultureGuessing)
            return;

        var nonEmptySamples = samples.Where(static s => !string.IsNullOrWhiteSpace(s));

        foreach (var sSample in nonEmptySamples)
        {
            var sample = sSample.AsSpan();
            total++;
            if (DateTime.TryParse(sample, Culture, DateTimeStyles.None, out _))
                simple++;
            else
            {
                _dateFormatToUse = DateFormatsDM;
                if (TryBruteParse(sample, out _))
                    d++;
                _dateFormatToUse = DateFormatsMD;
                if (TryBruteParse(sample, out _))
                    m++;
            }
        }

        if (simple < total && d > m)
            _dateFormatToUse = DateFormatsDM;
    }

    /// <inheritdoc />
    public override bool IsAcceptableAsType(ReadOnlySpan<char> candidateString, IDataTypeSize? size)
    {
        return IsExplicitDate(candidateString) || base.IsAcceptableAsType(candidateString, size);
    }

    /// <inheritdoc/>
    protected override bool IsAcceptableAsTypeCore(ReadOnlySpan<char> candidateString, IDataTypeSize? size)
    {
        //if it's a float then it isn't a date is it! thanks C# for thinking 1.1 is the first of January
        if (_decimalChecker.IsAcceptableAsType(candidateString, size))
            return false;

        //likewise if it is just the Time portion of the date then we have a column with mixed dates and times which SQL will not deal with well in the end database (e.g. it will set the
        //date portion of times to today's date which will be very confusing
        if (_timeSpanTypeDecider.IsAcceptableAsType(candidateString, size))
            return false;

        // TryBruteParse already handles all exceptions internally and returns false on failure
        return TryBruteParse(candidateString, out _);
    }

    private bool TryBruteParse(ReadOnlySpan<char> s, out DateTime dt)
    {
        //if it's legit according to the current culture
        if (DateTime.TryParse(s, Culture, DateTimeStyles.None, out dt))
            return true;

        //if there are no tokens
        if (s.IsEmpty)
        {
            dt=DateTime.MinValue;
            return false;
        }

        var sPoint = s.IndexOf(' ');

        //if there is one token it is assumed either to be a date or a string
        if (sPoint == -1)
        {
            return TryGetTime(s, out dt) || TryGetDate(s, out dt);
        }

        //if there are 2+ tokens then first token should be a date then the rest (concatenated) should be a time
        //e.g. "28/2/1993 5:36:27 AM" gets evaluated as "28/2/1993" and then "5:36:27 AM"

        if (TryGetDate(s[..sPoint], out dt) && TryGetTime(s[(sPoint+1)..], out var time))
        {
            dt = new DateTime(dt.Year, dt.Month, dt.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
            return true;
        }

        dt = DateTime.MinValue;
        return false;
    }

    private bool TryGetDate(ReadOnlySpan<char> v, out DateTime date)
    {
        return DateTime.TryParseExact(v, _dateFormatToUse, Culture, DateTimeStyles.AllowInnerWhite, out date);
    }

    private bool TryGetTime(ReadOnlySpan<char> v, out DateTime time)
    {
        return DateTime.TryParseExact(v, TimeFormats, Culture, DateTimeStyles.AllowInnerWhite, out time);
    }
}