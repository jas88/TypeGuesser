using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using TypeGuesser;

namespace Tests;

/// <summary>
/// <para>A <see cref="Guesser"/> constructed from a <see cref="DatabaseTypeRequest"/> (e.g. that of an existing database column) starts
/// out guessing that request: its width, decimal size and Unicode flag as well as its Type.  Values seen afterwards may only widen it.</para>
///
/// <para>Priming is not a value seen, and the width, decimal size and Unicode flag of the request do not change which values are
/// accepted.</para>
/// </summary>
public sealed class GuesserRequestTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void StringRequest_GuessStartsWithItsWidthAndUnicode(bool unicode)
    {
        // e.g. nvarchar(10) / varchar(10)
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(string), 10) { Unicode = unicode });
        var guess = guesser.Guess;

        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(guess.Width, Is.EqualTo(10));
            Assert.That(guess.Unicode, Is.EqualTo(unicode));
        });
    }

    [Test]
    public void UnlimitedStringRequest_KeepsItsWidth()
    {
        // e.g. nvarchar(max), which FAnsiSql requests as a width of int.MaxValue
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(string), int.MaxValue) { Unicode = true });
        Assert.That(guesser.Guess.Width, Is.EqualTo(int.MaxValue));

        guesser.AdjustToCompensateForValue("abc");
        guesser.AdjustToCompensateForValue(new string('x', 10000));

        var guess = guesser.Guess;
        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(guess.Width, Is.EqualTo(int.MaxValue));
            Assert.That(guess.Unicode, Is.True);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DecimalRequest_GuessStartsWithItsPrecisionAndScale(bool withStringWidth)
    {
        // decimal(10,5).  FAnsiSql also gives the request of a decimal(10,5) column the width of its string representation
        var request = new DatabaseTypeRequest(typeof(decimal), withStringWidth ? 11 : null, new DecimalSize(5, 5));
        using var guesser = new Guesser(request);
        var guess = guesser.Guess;

        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(guess.Size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(guess.Size.NumbersAfterDecimalPlace, Is.EqualTo(5));
            Assert.That(guess.Size.Precision, Is.EqualTo(10));
            Assert.That(guess.Size.Scale, Is.EqualTo(5));
            Assert.That(guess.Width, Is.EqualTo(11));
            Assert.That(guess, Is.EqualTo(request));
        });
    }

    [Test]
    public void StringRequest_SmallerValuesDoNotShrinkTheGuess()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(string), 10) { Unicode = true });

        guesser.AdjustToCompensateForValue("abc");
        guesser.AdjustToCompensateForValue("");
        guesser.AdjustToCompensateForValue(null);

        var guess = guesser.Guess;
        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(guess.Width, Is.EqualTo(10));
            Assert.That(guess.Unicode, Is.True, "ASCII values must not make a Unicode request non-Unicode");
        });
    }

    [Test]
    public void StringRequest_LargerValuesWidenTheGuess()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(string), 10));

        guesser.AdjustToCompensateForValue("abcdefghijklmno");
        Assert.Multiple(() =>
        {
            Assert.That(guesser.Guess.Width, Is.EqualTo(15));
            Assert.That(guesser.Guess.Unicode, Is.False);
        });

        guesser.AdjustToCompensateForValue("é");
        Assert.Multiple(() =>
        {
            Assert.That(guesser.Guess.Width, Is.EqualTo(15));
            Assert.That(guesser.Guess.Unicode, Is.True);
        });
    }

    [Test]
    public void DecimalRequest_SmallerValuesDoNotShrinkTheGuess()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));

        guesser.AdjustToCompensateForValue("1.5");
        guesser.AdjustToCompensateForValue("2");

        AssertGuessIsDecimal(guesser.Guess, 5, 5);
    }

    [Test]
    public void DecimalRequest_LargerValuesWidenTheGuess()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));

        guesser.AdjustToCompensateForValue("123456789.1");
        AssertGuessIsDecimal(guesser.Guess, 9, 5);

        guesser.AdjustToCompensateForValue("1.1234567");
        AssertGuessIsDecimal(guesser.Guess, 9, 7);
        Assert.That(guesser.Guess.Width, Is.EqualTo(17));
    }

    [Test]
    public void DecimalRequest_HardTypedDecimalsOnlyWidenTheGuess()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));

        guesser.AdjustToCompensateForValue(1.5m);
        AssertGuessIsDecimal(guesser.Guess, 5, 5);

        guesser.AdjustToCompensateForValue(1234567.1234567m);
        AssertGuessIsDecimal(guesser.Guess, 7, 7);
    }

    [Test]
    public void DecimalRequest_HardTypedIntsKeepTheRequestedDecimalSize()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));

        guesser.AdjustToCompensateForValue(7);

        var guess = guesser.Guess;
        Assert.Multiple(() =>
        {
            // a hard typed int is accepted as before (and its Type taken)...
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(int)));

            // ...but does not shrink the decimal size of the request
            Assert.That(guess.Size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(guess.Size.NumbersAfterDecimalPlace, Is.EqualTo(5));
        });
    }

    [Test]
    public void StringRequest_RejectsHardTypedValuesAsBefore()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(string), 10) { Unicode = true });

        Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue(5));
    }

    [Test]
    public void DecimalRequest_RejectsHardTypedValuesAfterStringsAsBefore()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));
        guesser.AdjustToCompensateForValue("1.5");

        Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue(1.5m));
    }

    [Test]
    public void DecimalRequest_RejectsStringsAfterHardTypedValuesAsBefore()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));
        guesser.AdjustToCompensateForValue(1.5m);

        Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue("1.5"));
    }

    [Test]
    public void Request_IsNotAValueSeen()
    {
        using var guesser = new Guesser(new DatabaseTypeRequest(typeof(decimal), 11, new DecimalSize(5, 5)) { Unicode = true });

        Assert.That(guesser.IsPrimedWithBonafideType, Is.False);
    }

    [Test]
    public void DefaultConstructor_GuessStartsEmpty()
    {
        using var guesser = new Guesser();
        AssertGuessIsEmptyBool(guesser.Guess);
    }

    [Test]
    public void DisposedGuesser_DoesNotPassItsRequestOnToTheNextGuesser()
    {
        // the builder of a disposed Guesser goes back into a pool shared by all Guessers
        var primed = new Guesser(new DatabaseTypeRequest(typeof(string), 10, new DecimalSize(5, 5)) { Unicode = true });
        primed.Dispose();

        using var guesser = new Guesser();
        AssertGuessIsEmptyBool(guesser.Guess);
    }

    /// <summary>
    /// The width, decimal size and Unicode flag of a request do not change which values a <see cref="Guesser"/> accepts: the same
    /// values are accepted or rejected (with a <see cref="MixedTypingException"/>), with the same guessed Type, as by the Guesser
    /// of a request with only that Type, whose behaviour is unchanged.
    /// </summary>
    [TestCaseSource(nameof(AcceptanceCases))]
    public void RequestSize_DoesNotChangeWhichValuesAreAccepted(Type requestType, object?[] values)
    {
        var typeOnly = Outcome(new DatabaseTypeRequest(requestType), values);
        var primed = Outcome(new DatabaseTypeRequest(requestType, 20, new DecimalSize(5, 5)) { Unicode = true }, values);

        Assert.That(primed, Is.EqualTo(typeOnly));
    }

    private static readonly Type[] _requestTypes =
    [
        typeof(string), typeof(bool), typeof(int), typeof(decimal), typeof(long), typeof(TimeSpan), typeof(DateTime),
        typeof(byte), typeof(short), typeof(Guid), typeof(byte[])
    ];

    private static readonly object?[][] _valueSequences =
    [
        [5],
        [5.5m],
        [true],
        [5L],
        [1.5d],
        [new DateTime(2001, 1, 1)],
        [Guid.Empty],
        ["5"],
        ["5.5"],
        ["abc"],
        ["2001-01-01"],
        [null, 5],
        ["   ", 5],
        ["5", 5],
        ["abc", 5],
        [5, "5"],
        [5, 5.5m],
        [5.5m, 5.5m]
    ];

    private static IEnumerable<TestCaseData> AcceptanceCases() =>
        from type in _requestTypes
        from values in _valueSequences
        select new TestCaseData(type, values).SetArgDisplayNames(type.Name, Describe(values));

    private static string Describe(object?[] values) =>
        string.Join(" then ", values.Select(v => v switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => $"{v.GetType().Name} {Convert.ToString(v, CultureInfo.InvariantCulture)}"
        }));

    /// <summary>
    /// Shows <paramref name="values"/> to a new <see cref="Guesser"/> of <paramref name="request"/>, recording whether each was
    /// accepted and the Type guessed
    /// </summary>
    private static string Outcome(DatabaseTypeRequest request, object?[] values)
    {
        using var guesser = new Guesser(request);
        var outcome = new List<string> { $"initially {guesser.Guess.CSharpType.Name}" };

        foreach (var value in values)
            try
            {
                guesser.AdjustToCompensateForValue(value);
                outcome.Add($"accepted, {guesser.Guess.CSharpType.Name}");
            }
            catch (MixedTypingException)
            {
                outcome.Add("rejected");
            }

        outcome.Add($"IsPrimedWithBonafideType={guesser.IsPrimedWithBonafideType}");
        return string.Join("; ", outcome);
    }

    private static void AssertGuessIsDecimal(DatabaseTypeRequest guess, int numbersBeforeDecimalPlace, int numbersAfterDecimalPlace)
    {
        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(guess.Size.NumbersBeforeDecimalPlace, Is.EqualTo(numbersBeforeDecimalPlace));
            Assert.That(guess.Size.NumbersAfterDecimalPlace, Is.EqualTo(numbersAfterDecimalPlace));
        });
    }

    private static void AssertGuessIsEmptyBool(DatabaseTypeRequest guess)
    {
        Assert.Multiple(() =>
        {
            Assert.That(guess.CSharpType, Is.EqualTo(typeof(bool)));
            Assert.That(guess.Width, Is.Null);
            Assert.That(guess.Size.IsEmpty, Is.True);
            Assert.That(guess.Unicode, Is.False);
        });
    }
}
