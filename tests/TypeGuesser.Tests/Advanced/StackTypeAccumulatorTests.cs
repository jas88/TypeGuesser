using System;
using System.Globalization;
using NUnit.Framework;
using TypeGuesser;
using TypeGuesser.Advanced;

namespace Tests.Advanced;

/// <summary>
/// Tests for the zero-allocation StackTypeAccumulator API.
/// </summary>
[TestFixture]
public class StackTypeAccumulatorTests
{
    private TypeDeciderFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
    }

    [Test]
    public void Add_Integers_DeterminesCorrectType()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(1);
        accumulator.Add(42);
        accumulator.Add(999);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(3)); // 999 has 3 digits
        Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(0));
    }

    [Test]
    public void Add_NegativeIntegers_IncludesSignInWidth()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(-1234);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(4)); // digits only
        Assert.That(result.Width, Is.EqualTo(5)); // includes negative sign
    }

    [Test]
    public void Add_Decimals_DeterminesCorrectPrecisionAndScale()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(1.99m);
        accumulator.Add(10.50m);
        accumulator.Add(999.999m);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(3)); // from 999.999
        Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(3)); // from 999.999
        Assert.That(result.Size.Precision, Is.EqualTo(6));
        Assert.That(result.Size.Scale, Is.EqualTo(3));
    }

    [Test]
    public void Add_Booleans_DeterminesCorrectType()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(true);
        accumulator.Add(false);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(bool)));
        Assert.That(result.Width, Is.EqualTo(5)); // "false" is longest
    }

    [Test]
    public void Add_MixedIntAndDecimal_UpgradesToDecimal()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(42);
        accumulator.Add(3.14m);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
    }

    [Test]
    public void Add_StringSpan_ParsesAsInteger()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add("123".AsSpan());
        accumulator.Add("456".AsSpan());

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(3));
    }

    [Test]
    public void Add_StringSpan_ParsesAsDecimal()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add("1.99".AsSpan());
        accumulator.Add("10.50".AsSpan());

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
        Assert.That(result.Size.Scale, Is.EqualTo(2));
    }

    [Test]
    public void Add_StringSpan_FallsBackToString()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add("123".AsSpan());
        accumulator.Add("not a number".AsSpan());

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
        Assert.That(result.Width, Is.EqualTo(12)); // length of "not a number"
    }

    [Test]
    public void Add_StringSpan_DetectsUnicode()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add("hello".AsSpan());
        accumulator.Add("café".AsSpan());

        var result = accumulator.GetResult();

        Assert.That(result.Unicode, Is.True);
    }

    [Test]
    public void Add_EmptySpan_IsIgnored()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(ReadOnlySpan<char>.Empty);
        accumulator.Add("   ".AsSpan()); // whitespace

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(string))); // No values seen
        Assert.That(result.Width, Is.Null.Or.EqualTo(0));
    }

    [Test]
    public void GetResult_CanBeCalledMultipleTimes()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(42);

        var result1 = accumulator.GetResult();
        var result2 = accumulator.GetResult();

        Assert.That(result1.CSharpType, Is.EqualTo(result2.CSharpType));
        Assert.That(result1.Size.NumbersBeforeDecimalPlace, Is.EqualTo(result2.Size.NumbersBeforeDecimalPlace));
    }

    [Test]
    public void Add_Zero_HandlesCorrectly()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(0);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(1));
    }

    [Test]
    public void Add_LargeNumbers_HandlesCorrectly()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(int.MaxValue);
        accumulator.Add(int.MinValue);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(10)); // Max int digits
    }

    [Test]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            new StackTypeAccumulator(null!);
        });
    }

    [Test]
    public void Add_DecimalZero_HandlesCorrectly()
    {
        var accumulator = new StackTypeAccumulator(_factory);

        accumulator.Add(0.00m);

        var result = accumulator.GetResult();

        Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
    }
}
