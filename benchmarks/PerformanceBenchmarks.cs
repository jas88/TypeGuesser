using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Globalization;
using TypeGuesser;

namespace TypeGuesser.Benchmarks;

/// <summary>
/// Benchmarks focused on raw performance and throughput.
/// Compares different input types, data sizes, and configuration options.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class PerformanceBenchmarks
{
    private int[] _integers = null!;
    private string[] _integerStrings = null!;
    private decimal[] _decimals = null!;
    private string[] _decimalStrings = null!;
    private bool[] _bools = null!;
    private string[] _boolStrings = null!;
    private TimeSpan[] _timeSpans = null!;
    private string[] _timeSpanStrings = null!;

    [Params(1_000, 10_000, 100_000, 1_000_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        // Integer datasets
        _integers = new int[N];
        _integerStrings = new string[N];
        for (var i = 0; i < N; i++)
        {
            _integers[i] = random.Next(-1000000, 1000000);
            _integerStrings[i] = _integers[i].ToString();
        }

        // Decimal datasets
        _decimals = new decimal[N];
        _decimalStrings = new string[N];
        for (var i = 0; i < N; i++)
        {
            _decimals[i] = (decimal)(random.NextDouble() * 100000 - 50000);
            _decimalStrings[i] = _decimals[i].ToString("F6");
        }

        // Boolean datasets
        _bools = new bool[N];
        _boolStrings = new string[N];
        for (var i = 0; i < N; i++)
        {
            _bools[i] = random.Next(2) == 0;
            _boolStrings[i] = _bools[i] ? "true" : "false";
        }

        // TimeSpan datasets
        _timeSpans = new TimeSpan[N];
        _timeSpanStrings = new string[N];
        for (var i = 0; i < N; i++)
        {
            _timeSpans[i] = TimeSpan.FromSeconds(random.Next(0, 86400));
            _timeSpanStrings[i] = _timeSpans[i].ToString(@"hh\:mm\:ss");
        }
    }

    #region Throughput Tests

    [Benchmark(Description = "Throughput: Hard-typed integers")]
    public DatabaseTypeRequest ThroughputHardTypedIntegers()
    {
        var guesser = new Guesser();
        foreach (var value in _integers)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: String integers")]
    public DatabaseTypeRequest ThroughputStringIntegers()
    {
        var guesser = new Guesser();
        foreach (var value in _integerStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: Hard-typed decimals")]
    public DatabaseTypeRequest ThroughputHardTypedDecimals()
    {
        var guesser = new Guesser();
        foreach (var value in _decimals)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: String decimals")]
    public DatabaseTypeRequest ThroughputStringDecimals()
    {
        var guesser = new Guesser();
        foreach (var value in _decimalStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: Hard-typed booleans")]
    public DatabaseTypeRequest ThroughputHardTypedBooleans()
    {
        var guesser = new Guesser();
        foreach (var value in _bools)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: String booleans")]
    public DatabaseTypeRequest ThroughputStringBooleans()
    {
        var guesser = new Guesser();
        foreach (var value in _boolStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: Hard-typed TimeSpans")]
    public DatabaseTypeRequest ThroughputHardTypedTimeSpans()
    {
        var guesser = new Guesser();
        foreach (var value in _timeSpans)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Throughput: String TimeSpans")]
    public DatabaseTypeRequest ThroughputStringTimeSpans()
    {
        var guesser = new Guesser();
        foreach (var value in _timeSpanStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    #endregion

    #region Culture Comparison

    [Benchmark(Description = "Culture: InvariantCulture")]
    public DatabaseTypeRequest CultureInvariant()
    {
        var guesser = new Guesser
        {
            Culture = CultureInfo.InvariantCulture
        };
        foreach (var value in _decimalStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Culture: en-US")]
    public DatabaseTypeRequest CultureEnUs()
    {
        var guesser = new Guesser
        {
            Culture = CultureInfo.GetCultureInfo("en-US")
        };
        foreach (var value in _decimalStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Culture: de-DE (different decimal separator)")]
    public DatabaseTypeRequest CultureDeDe()
    {
        var guesser = new Guesser
        {
            Culture = CultureInfo.GetCultureInfo("de-DE")
        };

        // Note: Our test data uses '.' as decimal separator
        // This will force fallback to string, testing that path
        foreach (var value in _decimalStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    #endregion

    #region Parse Performance

    [Benchmark(Description = "Parse: Integer strings to hard type")]
    public object[] ParseIntegerStrings()
    {
        var guesser = new Guesser();
        guesser.AdjustToCompensateForValues(_integerStrings);

        var results = new object[_integerStrings.Length];
        for (var i = 0; i < _integerStrings.Length; i++)
        {
            results[i] = guesser.Parse(_integerStrings[i])!;
        }
        return results;
    }

    [Benchmark(Description = "Parse: Decimal strings to hard type")]
    public object[] ParseDecimalStrings()
    {
        var guesser = new Guesser();
        guesser.AdjustToCompensateForValues(_decimalStrings);

        var results = new object[_decimalStrings.Length];
        for (var i = 0; i < _decimalStrings.Length; i++)
        {
            results[i] = guesser.Parse(_decimalStrings[i])!;
        }
        return results;
    }

    [Benchmark(Description = "Parse: TimeSpan strings to hard type")]
    public object[] ParseTimeSpanStrings()
    {
        var guesser = new Guesser();
        guesser.AdjustToCompensateForValues(_timeSpanStrings);

        var results = new object[_timeSpanStrings.Length];
        for (var i = 0; i < _timeSpanStrings.Length; i++)
        {
            results[i] = guesser.Parse(_timeSpanStrings[i])!;
        }
        return results;
    }

    #endregion

    #region GuessSettings Performance

    [Benchmark(Description = "Settings: Default GuessSettings")]
    public DatabaseTypeRequest SettingsDefault()
    {
        var guesser = new Guesser();
        foreach (var value in _boolStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    [Benchmark(Description = "Settings: CharCanBeBoolean = true")]
    public DatabaseTypeRequest SettingsCharAsBoolean()
    {
        var guesser = new Guesser();
        guesser.Settings.CharCanBeBoolean = true;

        var yesNoStrings = new[] { "Y", "N", "Y", "Y", "N" };
        foreach (var value in yesNoStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    #endregion

    #region Real-World Scenarios

    /// <summary>
    /// Simulates processing a DataTable column with 90% valid integers, 10% nulls.
    /// </summary>
    [Benchmark(Description = "Scenario: DataTable with nulls")]
    public DatabaseTypeRequest ScenarioDataTableWithNulls()
    {
        var guesser = new Guesser();
        for (var i = 0; i < _integers.Length; i++)
        {
            if (i % 10 == 0)
            {
                guesser.AdjustToCompensateForValue(null);
            }
            else
            {
                guesser.AdjustToCompensateForValue(_integers[i]);
            }
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Simulates CSV processing with whitespace-padded values.
    /// </summary>
    [Benchmark(Description = "Scenario: CSV with whitespace")]
    public DatabaseTypeRequest ScenarioCsvWithWhitespace()
    {
        var guesser = new Guesser();
        for (var i = 0; i < _integerStrings.Length; i++)
        {
            var index = i % 3;
            var paddedValue = index switch
            {
                0 => $" {_integerStrings[i]}",
                1 => $"{_integerStrings[i]} ",
                _ => _integerStrings[i]
            };
            guesser.AdjustToCompensateForValue(paddedValue);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Simulates processing mixed valid/invalid data requiring fallback.
    /// </summary>
    [Benchmark(Description = "Scenario: Mixed valid/invalid requiring fallback")]
    public DatabaseTypeRequest ScenarioMixedWithFallback()
    {
        var guesser = new Guesser();

        // Process mostly valid integers
        for (var i = 0; i < _integerStrings.Length - 1; i++)
        {
            guesser.AdjustToCompensateForValue(_integerStrings[i]);
        }

        // One invalid value forces string fallback
        guesser.AdjustToCompensateForValue("INVALID");

        return guesser.Guess;
    }

    /// <summary>
    /// Simulates progressive type refinement as more data is seen.
    /// </summary>
    [Benchmark(Description = "Scenario: Progressive type refinement")]
    public DatabaseTypeRequest ScenarioProgressiveRefinement()
    {
        var guesser = new Guesser();

        // Start with booleans
        guesser.AdjustToCompensateForValue("true");
        guesser.AdjustToCompensateForValue("false");

        // Add integers (should force to string since incompatible)
        for (var i = 0; i < 100; i++)
        {
            guesser.AdjustToCompensateForValue(_integerStrings[i]);
        }

        return guesser.Guess;
    }

    #endregion
}
