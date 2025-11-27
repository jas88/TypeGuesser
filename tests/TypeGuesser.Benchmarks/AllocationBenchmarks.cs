using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using TypeGuesser;

namespace TypeGuesser.Benchmarks;

/// <summary>
/// Benchmarks focused on proving zero-allocation performance for hard-typed inputs.
/// Compares allocation patterns between string-based and hard-typed processing.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class AllocationBenchmarks
{
    private int[] _integers = null!;
    private string[] _integerStrings = null!;
    private decimal[] _decimals = null!;
    private string[] _decimalStrings = null!;
    private DateTime[] _dateTimes = null!;
    private string[] _dateTimeStrings = null!;
    private object[] _mixedObjects = null!;

    [Params(1000, 10000, 100000, 1000000)]
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
            _integers[i] = random.Next(-100000, 100000);
            _integerStrings[i] = _integers[i].ToString();
        }

        // Decimal datasets
        _decimals = new decimal[N];
        _decimalStrings = new string[N];
        for (var i = 0; i < N; i++)
        {
            _decimals[i] = (decimal)(random.NextDouble() * 10000 - 5000);
            _decimalStrings[i] = _decimals[i].ToString("F3");
        }

        // DateTime datasets
        _dateTimes = new DateTime[N];
        _dateTimeStrings = new string[N];
        var baseDate = new DateTime(2000, 1, 1);
        for (var i = 0; i < N; i++)
        {
            _dateTimes[i] = baseDate.AddDays(random.Next(0, 10000));
            _dateTimeStrings[i] = _dateTimes[i].ToString("yyyy-MM-dd");
        }

        // Mixed object dataset (integers, decimals, dates)
        _mixedObjects = new object[N];
        for (var i = 0; i < N; i++)
        {
            _mixedObjects[i] = (i % 3) switch
            {
                0 => random.Next(-10000, 10000),
                1 => (decimal)(random.NextDouble() * 1000),
                _ => baseDate.AddDays(random.Next(0, 10000))
            };
        }
    }

    /// <summary>
    /// BASELINE: Process hard-typed integers.
    /// Expected: ZERO allocations (except the Guesser instance itself).
    /// </summary>
    [Benchmark(Baseline = true, Description = "Process hard-typed integers (ZERO allocation target)")]
    public DatabaseTypeRequest ProcessHardTypedIntegers()
    {
        var guesser = new Guesser();
        foreach (var value in _integers)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Compare string processing of same integers.
    /// Expected: String allocations for parsing.
    /// </summary>
    [Benchmark(Description = "Process integer strings (has allocations)")]
    public DatabaseTypeRequest ProcessIntegerStrings()
    {
        var guesser = new Guesser();
        foreach (var value in _integerStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Process hard-typed decimals.
    /// Expected: ZERO allocations beyond Guesser instance.
    /// </summary>
    [Benchmark(Description = "Process hard-typed decimals (ZERO allocation target)")]
    public DatabaseTypeRequest ProcessHardTypedDecimals()
    {
        var guesser = new Guesser();
        foreach (var value in _decimals)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Compare string processing of same decimals.
    /// Expected: String allocations for parsing.
    /// </summary>
    [Benchmark(Description = "Process decimal strings (has allocations)")]
    public DatabaseTypeRequest ProcessDecimalStrings()
    {
        var guesser = new Guesser();
        foreach (var value in _decimalStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Process hard-typed DateTimes.
    /// Expected: ZERO allocations beyond Guesser instance.
    /// </summary>
    [Benchmark(Description = "Process hard-typed DateTimes (ZERO allocation target)")]
    public DatabaseTypeRequest ProcessHardTypedDateTimes()
    {
        var guesser = new Guesser();
        foreach (var value in _dateTimes)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Compare string processing of same dates.
    /// Expected: String allocations for parsing.
    /// </summary>
    [Benchmark(Description = "Process DateTime strings (has allocations)")]
    public DatabaseTypeRequest ProcessDateTimeStrings()
    {
        var guesser = new Guesser();
        foreach (var value in _dateTimeStrings)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Process mixed hard-typed objects.
    /// Expected: ZERO allocations beyond Guesser instance.
    /// This demonstrates the real-world scenario of processing DataTable columns.
    /// </summary>
    [Benchmark(Description = "Process mixed hard-typed objects (ZERO allocation target)")]
    public DatabaseTypeRequest ProcessMixedHardTyped()
    {
        var guesser = new Guesser();
        foreach (var value in _mixedObjects)
        {
            guesser.AdjustToCompensateForValue(value);
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Test with fallback scenario: integers that eventually require string.
    /// </summary>
    [Benchmark(Description = "Process with fallback to string")]
    public DatabaseTypeRequest ProcessWithFallback()
    {
        var guesser = new Guesser();

        // Process N-1 integers
        for (var i = 0; i < _integerStrings.Length - 1; i++)
        {
            guesser.AdjustToCompensateForValue(_integerStrings[i]);
        }

        // Last value forces fallback to string
        guesser.AdjustToCompensateForValue("not a number");

        return guesser.Guess;
    }

    /// <summary>
    /// Simulate real-world CSV processing scenario.
    /// Large dataset of numeric strings with occasional nulls.
    /// </summary>
    [Benchmark(Description = "CSV-like scenario with nulls")]
    public DatabaseTypeRequest CsvScenarioWithNulls()
    {
        var guesser = new Guesser();
        for (var i = 0; i < _integerStrings.Length; i++)
        {
            // Every 100th value is null
            if (i % 100 == 0)
            {
                guesser.AdjustToCompensateForValue(null);
            }
            else
            {
                guesser.AdjustToCompensateForValue(_integerStrings[i]);
            }
        }
        return guesser.Guess;
    }

    /// <summary>
    /// Test reusing a Guesser instance (demonstrates pooling scenario).
    /// </summary>
    [Benchmark(Description = "Reuse single Guesser instance")]
    public DatabaseTypeRequest ReuseGuesserInstance()
    {
        var guesser = new Guesser();

        // First batch
        foreach (var value in _integers.AsSpan(0, _integers.Length / 2))
        {
            guesser.AdjustToCompensateForValue(value);
        }

        var firstResult = guesser.Guess;

        // Note: In real usage you'd create a new Guesser for new data
        // This benchmark shows the overhead of the check

        return firstResult;
    }
}
