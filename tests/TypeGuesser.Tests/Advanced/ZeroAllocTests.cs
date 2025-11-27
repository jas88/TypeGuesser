using System;
using System.Globalization;
using NUnit.Framework;
using TypeGuesser;
using TypeGuesser.Advanced;

namespace Tests.Advanced;

/// <summary>
/// Tests for the zero-allocation convenience methods in the ZeroAlloc static class.
/// </summary>
[TestFixture]
public class ZeroAllocTests
{
    [Test]
    public void GuessIntegers_Array_DeterminesCorrectType()
    {
        var numbers = new int[] { 1, 42, 999, -1234 };

        var result = ZeroAlloc.GuessIntegers(numbers);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(4)); // from -1234
        Assert.That(result.Width, Is.EqualTo(5)); // includes negative sign
    }

    [Test]
    public void GuessIntegers_Span_DeterminesCorrectType()
    {
        Span<int> numbers = stackalloc int[] { 100, 200, 300 };

        var result = ZeroAlloc.GuessIntegers(numbers);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(3)); // from 300
    }

    [Test]
    public void GuessIntegers_EmptySpan_ReturnsStringType()
    {
        ReadOnlySpan<int> empty = ReadOnlySpan<int>.Empty;

        var result = ZeroAlloc.GuessIntegers(empty);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GuessIntegers_SingleValue_Works()
    {
        var numbers = new int[] { 42 };

        var result = ZeroAlloc.GuessIntegers(numbers);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(2));
    }

    [Test]
    public void GuessDecimals_Array_DeterminesCorrectType()
    {
        var values = new decimal[] { 1.99m, 10.50m, 999.999m };

        var result = ZeroAlloc.GuessDecimals(values);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(3)); // from 999.999
        Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(3)); // from 999.999
        Assert.That(result.Size.Precision, Is.EqualTo(6));
        Assert.That(result.Size.Scale, Is.EqualTo(3));
    }

    [Test]
    public void GuessDecimals_Span_DeterminesCorrectType()
    {
        Span<decimal> values = stackalloc decimal[] { 1.1m, 2.22m, 3.333m };

        var result = ZeroAlloc.GuessDecimals(values);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.Scale, Is.EqualTo(3)); // max scale
    }

    [Test]
    public void GuessDecimals_FinancialData_Works()
    {
        var transactions = new decimal[] { 100.00m, -50.25m, 1234.56m };

        var result = ZeroAlloc.GuessDecimals(transactions);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(4)); // from 1234.56
        Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(2));
    }

    [Test]
    public void GuessBooleans_Array_DeterminesCorrectType()
    {
        var flags = new bool[] { true, false, true, true };

        var result = ZeroAlloc.GuessBooleans(flags);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(bool)));
        Assert.That(result.Width, Is.EqualTo(5)); // length of "false"
    }

    [Test]
    public void GuessBooleans_Span_Works()
    {
        Span<bool> values = stackalloc bool[] { true, false };

        var result = ZeroAlloc.GuessBooleans(values);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void CreateAccumulator_ReturnsValidAccumulator()
    {
        var accumulator = ZeroAlloc.CreateAccumulator();

        accumulator.Add(42);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void CreateAccumulator_WithCustomFactory_UsesFactory()
    {
        var factory = new TypeDeciderFactory(new CultureInfo("de-DE"));
        var accumulator = ZeroAlloc.CreateAccumulator(factory);

        accumulator.Add(42);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void CreateAccumulator_HandlesMixedTypes()
    {
        var accumulator = ZeroAlloc.CreateAccumulator();

        accumulator.Add(true);       // Type: bool
        accumulator.Add(42);         // Type upgrades to: int
        accumulator.Add(3.14m);      // Type upgrades to: decimal

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.Scale, Is.EqualTo(2));
    }

    [Test]
    public void GuessIntegers_LargeArray_Handles()
    {
        var largeArray = new int[10000];
        for (int i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = i;
        }

        var result = ZeroAlloc.GuessIntegers(largeArray);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(4)); // 9999 has 4 digits
    }

    [Test]
    public void GuessDecimals_VariedPrecision_FindsMaximum()
    {
        var values = new decimal[]
        {
            1m,           // precision 1, scale 0
            1.1m,         // precision 2, scale 1
            1.12m,        // precision 3, scale 2
            1.123m,       // precision 4, scale 3
            12.1234m      // precision 6, scale 4
        };

        var result = ZeroAlloc.GuessDecimals(values);

        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(2)); // from 12.1234
        Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(4)); // from 12.1234
    }

    [Test]
    public void GuessIntegers_WithFactory_UsesFactory()
    {
        var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
        var numbers = new int[] { 1, 2, 3 };

        var result = ZeroAlloc.GuessIntegers(numbers, factory);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GuessIntegers_AllZeros_Works()
    {
        var zeros = new int[] { 0, 0, 0 };

        var result = ZeroAlloc.GuessIntegers(zeros);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(1));
    }

    [Test]
    public void GuessDecimals_AllZeros_Works()
    {
        var zeros = new decimal[] { 0.0m, 0.00m };

        var result = ZeroAlloc.GuessDecimals(zeros);

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
    }

    [Test]
    public void CreateAccumulator_ProcessesMultipleBatches()
    {
        var accumulator = ZeroAlloc.CreateAccumulator();

        // Batch 1: booleans
        foreach (var flag in new[] { true, false, true })
            accumulator.Add(flag);

        // Batch 2: integers
        foreach (var count in new[] { 1, 10, 100 })
            accumulator.Add(count);

        // Batch 3: decimals
        foreach (var price in new[] { 9.99m, 19.99m })
            accumulator.Add(price);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
    }
}
