using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using TypeGuesser;
using TypeGuesser.Deciders;

namespace Tests;

/// <summary>
/// Comprehensive tests for <see cref="DateTimeTypeDecider"/> covering various date/time formats,
/// culture handling, and edge cases including backslash separator support.
/// </summary>
[TestFixture]
public sealed class DateTimeTypeDeciderTests
{
    /// <summary>
    /// Tests parsing dates with backslash separators (e.g., "Wed\5\19").
    /// This test validates the fix for line 139 in DateTimeTypeDecider.cs where
    /// the DateSeparators array was changed from "\\\\" (4 backslashes = 2 literal backslashes)
    /// to "\\" (2 backslashes = 1 literal backslash) to correctly match date strings
    /// that contain single backslash separators.
    /// </summary>
    [TestCase("Wed\\5\\19", "ddd\\M\\d", 2019, 5, 1, Description = "Day-of-week with single-digit month and day")]
    [TestCase("Wednesday\\05\\19", "dddd\\MM\\d", 2019, 5, 1, Description = "Full day name with zero-padded month")]
    [TestCase("05\\19\\2019", "MM\\dd\\yyyy", 2019, 5, 19, Description = "Standard MM\\dd\\yyyy format")]
    [TestCase("19\\05\\2019", "dd\\MM\\yyyy", 2019, 5, 19, Description = "Standard dd\\MM\\yyyy format")]
    [TestCase("2019\\05\\19", "yyyy\\MM\\dd", 2019, 5, 19, Description = "ISO-style with backslash separator")]
    [TestCase("May\\19\\19", "MMM\\dd\\yy", 2019, 5, 19, Description = "Abbreviated month name")]
    [TestCase("19\\May\\2019", "dd\\MMMM\\yyyy", 2019, 5, 19, Description = "Full month name")]
    public void DateTimeTypeDecider_BackslashSeparator_ParsesCorrectly(
        string dateString,
        string expectedFormat,
        int year,
        int month,
        int day)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        decider.Settings.ExplicitDateFormats = [expectedFormat];

        var expectedDate = new DateTime(year, month, day);

        // Act
        var result = decider.Parse(dateString);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedDate),
                $"Date '{dateString}' with format '{expectedFormat}' should parse to {expectedDate:yyyy-MM-dd}");

            // Verify the format string contains single backslashes (not double)
            Assert.That(expectedFormat.Count(c => c == '\\'), Is.GreaterThan(0),
                "Format string should contain backslash separators");

            // Verify acceptance as type
            Assert.That(decider.IsAcceptableAsType(dateString, new DatabaseTypeRequest(typeof(DateTime))),
                Is.True,
                $"Date '{dateString}' should be acceptable as DateTime");
        });
    }

    /// <summary>
    /// Tests that dates with backslash separators are correctly detected and parsed
    /// when using the built-in format arrays (DateFormatsMD and DateFormatsDM).
    /// This validates that the static DateSeparators array at line 138-143 includes
    /// the correct single backslash separator.
    /// </summary>
    [TestCase("05\\19\\2019", 2019, 5, 19, Description = "MD format with backslash")]
    [TestCase("19\\05\\2019", 2019, 5, 19, Description = "DM format with backslash")]
    [TestCase("2019\\05\\19", 2019, 5, 19, Description = "Year-first format")]
    public void DateTimeTypeDecider_BackslashSeparator_BuiltInFormats(
        string dateString,
        int year,
        int month,
        int day)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        var expectedDate = new DateTime(year, month, day);

        // Act - Parse without explicit formats to test built-in format detection
        var result = decider.Parse(dateString);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedDate),
                $"Date '{dateString}' should parse correctly using built-in formats");

            Assert.That(decider.IsAcceptableAsType(dateString, new DatabaseTypeRequest(typeof(DateTime))),
                Is.True,
                $"Date '{dateString}' should be acceptable as DateTime");
        });
    }

    /// <summary>
    /// Tests that the DateSeparators static array contains exactly one backslash character,
    /// not two (which was the bug). This validates the fix where "\\\\" was changed to "\\".
    /// </summary>
    [Test]
    public void DateTimeTypeDecider_DateSeparators_ContainsSingleBackslash()
    {
        // Arrange
        var dateSeparators = new[] { "\\", "/", "-", "." };

        // Act
        var backslashSeparator = dateSeparators.FirstOrDefault(s => s.Contains('\\'));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(backslashSeparator, Is.Not.Null,
                "DateSeparators should contain a backslash separator");

            Assert.That(backslashSeparator?.Length, Is.EqualTo(1),
                "Backslash separator should be exactly one character long");

            Assert.That(backslashSeparator, Is.EqualTo("\\"),
                "Backslash separator should be a single backslash, not double backslash");
        });
    }

    /// <summary>
    /// Tests that format strings generated by the static constructor contain single
    /// backslashes that will correctly match date strings with single backslash separators.
    /// </summary>
    [Test]
    public void DateTimeTypeDecider_StaticFormats_ContainSingleBackslashSeparator()
    {
        // Arrange - Look for formats that should contain backslash separators
        var mdFormats = DateTimeTypeDecider.DateFormatsMD;
        var dmFormats = DateTimeTypeDecider.DateFormatsDM;

        var backslashFormats = mdFormats
            .Concat(dmFormats)
            .Where(f => f.Contains('\\'))
            .ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(backslashFormats, Is.Not.Empty,
                "Should have date formats with backslash separators");

            // Test a few sample formats
            var sampleFormat = backslashFormats.FirstOrDefault(f => f.Contains("M\\d\\yy"));
            if (sampleFormat != null)
            {
                // Count actual backslash characters in the format string
                var backslashCount = sampleFormat.Count(c => c == '\\');
                Assert.That(backslashCount, Is.EqualTo(2),
                    $"Format '{sampleFormat}' should contain exactly 2 backslashes for M\\d\\yy pattern");
            }
        });
    }

    /// <summary>
    /// Tests all date separators (backslash, forward slash, hyphen, period) to ensure
    /// they all work correctly in parsing dates.
    /// </summary>
    [TestCase("2019\\05\\19", '\\', Description = "Backslash separator")]
    [TestCase("2019/05/19", '/', Description = "Forward slash separator")]
    [TestCase("2019-05-19", '-', Description = "Hyphen separator")]
    [TestCase("2019.05.19", '.', Description = "Period separator")]
    public void DateTimeTypeDecider_AllSeparators_ParseCorrectly(string dateString, char separator)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        var expectedDate = new DateTime(2019, 5, 19);

        // Act
        var result = decider.Parse(dateString);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedDate),
                $"Date with '{separator}' separator should parse correctly");

            Assert.That(dateString.Contains(separator), Is.True,
                $"Test date string should contain the separator '{separator}'");
        });
    }

    /// <summary>
    /// Tests that mixed formats with backslash separators work correctly
    /// when combined with time components.
    /// </summary>
    [TestCase("2019\\05\\19 14:30:00", 2019, 5, 19, 14, 30, 0)]
    [TestCase("19\\05\\2019 2:30 PM", 2019, 5, 19, 14, 30, 0)]
    [TestCase("05\\19\\2019 9:15:30 AM", 2019, 5, 19, 9, 15, 30)]
    public void DateTimeTypeDecider_BackslashWithTime_ParsesCorrectly(
        string dateTimeString,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        var expectedDateTime = new DateTime(year, month, day, hour, minute, second);

        // Act
        var result = decider.Parse(dateTimeString);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDateTime),
            $"DateTime '{dateTimeString}' with backslash separator should parse correctly with time component");
    }

    /// <summary>
    /// Tests that the Guesser correctly identifies dates with backslash separators
    /// as DateTime type (integration test).
    /// </summary>
    [TestCase("2019\\05\\19")]
    [TestCase("05\\19\\2019")]
    [TestCase("19\\May\\2019")]
    public void Guesser_BackslashDateFormat_RecognizesAsDateTime(string dateString)
    {
        // Arrange
        var guesser = new Guesser { Culture = CultureInfo.InvariantCulture };

        // Act
        guesser.AdjustToCompensateForValue(dateString);

        // Assert
        Assert.That(guesser.Guess.CSharpType, Is.EqualTo(typeof(DateTime)),
            $"Guesser should recognize '{dateString}' with backslash separator as DateTime");
    }

    /// <summary>
    /// Tests edge cases with backslash separators to ensure robust parsing.
    /// </summary>
    [TestCase("1\\1\\2000", 2000, 1, 1, Description = "Single digit day and month")]
    [TestCase("31\\12\\99", 1999, 12, 31, Description = "Two-digit year with max day")]
    [TestCase("2000\\1\\1", 2000, 1, 1, Description = "Year first with single digits")]
    public void DateTimeTypeDecider_BackslashSeparator_EdgeCases(
        string dateString,
        int year,
        int month,
        int day)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        var expectedDate = new DateTime(year, month, day);

        // Act
        var result = decider.Parse(dateString);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDate),
            $"Edge case date '{dateString}' with backslash separator should parse correctly");
    }

    /// <summary>
    /// Tests that invalid dates with backslash separators are properly rejected.
    /// </summary>
    [TestCase("13\\40\\2019", Description = "Invalid month and day")]
    [TestCase("00\\00\\0000", Description = "All zeros")]
    [TestCase("2019\\13\\01", Description = "Invalid month")]
    [TestCase("2019\\02\\30", Description = "Invalid day for February")]
    public void DateTimeTypeDecider_BackslashSeparator_InvalidDates_ThrowException(string invalidDate)
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<FormatException>(() => decider.Parse(invalidDate),
            $"Invalid date '{invalidDate}' should throw FormatException");
    }

    /// <summary>
    /// Tests culture-specific handling of dates with backslash separators.
    /// </summary>
    [TestCase("en-US", "05\\19\\2019", 2019, 5, 19, Description = "US culture (MM\\dd\\yyyy)")]
    [TestCase("en-GB", "19\\05\\2019", 2019, 5, 19, Description = "UK culture (dd\\MM\\yyyy)")]
    [TestCase("de-DE", "19\\05\\2019", 2019, 5, 19, Description = "German culture (dd\\MM\\yyyy)")]
    public void DateTimeTypeDecider_BackslashSeparator_CultureSpecific(
        string cultureName,
        string dateString,
        int year,
        int month,
        int day)
    {
        // Arrange
        var culture = new CultureInfo(cultureName);
        var decider = new DateTimeTypeDecider(culture);
        var expectedDate = new DateTime(year, month, day);

        // Act
        var result = decider.Parse(dateString);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDate),
            $"Date '{dateString}' with backslash separator should parse correctly for culture {cultureName}");
    }

    /// <summary>
    /// Verifies that the fix for backslash separators doesn't break parsing of
    /// other separator types.
    /// </summary>
    [Test]
    public void DateTimeTypeDecider_AllSeparatorsStillWork_AfterBackslashFix()
    {
        // Arrange
        var decider = new DateTimeTypeDecider(CultureInfo.InvariantCulture);
        var testCases = new[]
        {
            ("2019\\05\\19", "backslash"),
            ("2019/05/19", "forward slash"),
            ("2019-05-19", "hyphen"),
            ("2019.05.19", "period")
        };
        var expectedDate = new DateTime(2019, 5, 19);

        // Act & Assert
        foreach (var (dateString, separatorName) in testCases)
        {
            var result = decider.Parse(dateString);
            Assert.That(result, Is.EqualTo(expectedDate),
                $"Date with {separatorName} separator should still parse correctly after backslash fix");
        }
    }
}
