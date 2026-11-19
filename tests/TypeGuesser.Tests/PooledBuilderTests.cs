using System;
using System.Globalization;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class PooledBuilderTests
    {
        private PooledBuilder _builder = new(CultureInfo.InvariantCulture);

        [SetUp]
        public void SetUp()
        {
            _builder = new PooledBuilder(CultureInfo.InvariantCulture);
        }

        [Test]
        public void Constructor_WithCulture_SetsCultureAndInitializesCorrectly()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");

            // Act
            var builder = new PooledBuilder(culture);

            // Assert
            Assert.That(builder.Culture, Is.EqualTo(culture));
            Assert.That(builder.Settings, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullCulture_UsesCurrentCulture()
        {
            // Act
            var builder = new PooledBuilder(null!);

            // Assert
            Assert.That(builder.Culture, Is.EqualTo(CultureInfo.CurrentCulture));
        }

        [Test]
        public void Reset_RestoresInitialState()
        {
            // Arrange - process some values to change state
            _builder.ProcessIntZeroAlloc(42);
            _builder.ProcessIntZeroAlloc(100);

            // Verify state was changed
            var resultBeforeReset = _builder.Build();
            Assert.That(resultBeforeReset.ValueCount, Is.GreaterThan(0));
            Assert.That(resultBeforeReset.CSharpType, Is.EqualTo(typeof(int)));

            // Act - reset the builder
            _builder.Reset();

            // Assert - verify state is restored to initial
            var result = _builder.Build();
            Assert.That(result.CSharpType, Is.EqualTo(typeof(bool))); // First in preference order
            Assert.That(result.ValueCount, Is.EqualTo(0));
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.Width, Is.Null);
        }

        [Test]
        public void SetCulture_UpdatesCultureAndTypeDeciders()
        {
            // Arrange
            var newCulture = new CultureInfo("es-ES");

            // Act
            _builder.SetCulture(newCulture);

            // Assert
            Assert.That(_builder.Culture, Is.EqualTo(newCulture));
        }

        [Test]
        public void SetCulture_WithNullCulture_UsesCurrentCulture()
        {
            // Act
            _builder.SetCulture(null!);

            // Assert
            Assert.That(_builder.Culture, Is.EqualTo(CultureInfo.CurrentCulture));
        }

        [Test]
        public void Build_WithEmptyState_ReturnsDefaultResult()
        {
            // Act
            var result = _builder.Build();

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(bool))); // First in preference order
            Assert.That(result.ValueCount, Is.EqualTo(0));
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.Width, Is.Null);
            Assert.That(result.RequiresUnicode, Is.False);
        }

        [Test]
        public void ProcessIntZeroAlloc_SingleValue_UpdatesStateCorrectly()
        {
            // Act
            _builder.ProcessIntZeroAlloc(42);

            // Assert
            var result = _builder.Build();
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.ValueCount, Is.EqualTo(1));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(2)); // "42"
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(0));
            Assert.That(result.Width, Is.EqualTo(2));
        }

        [Test]
        public void ProcessIntZeroAlloc_ZeroValue_HandlesCorrectly()
        {
            // Act
            _builder.ProcessIntZeroAlloc(0);

            // Assert
            var result = _builder.Build();
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(1)); // "0"
            Assert.That(result.Width, Is.EqualTo(1));
        }

        [Test]
        public void ProcessIntZeroAlloc_NegativeValue_HandlesCorrectly()
        {
            // Act
            _builder.ProcessIntZeroAlloc(-123);

            // Assert
            var result = _builder.Build();
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(3)); // "123"
            Assert.That(result.Width, Is.EqualTo(4)); // "-123"
        }

        [Test]
        public void ProcessDecimalZeroAlloc_SingleValue_UpdatesStateCorrectly()
        {
            // Act
            _builder.ProcessDecimalZeroAlloc(12.34m);

            // Assert
            var result = _builder.Build();
            Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(result.ValueCount, Is.EqualTo(1));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(2)); // "12"
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(2)); // "34"
        }

        [Test]
        public void ProcessBoolZeroAlloc_TrueValue_UpdatesStateCorrectly()
        {
            // Act
            _builder.ProcessBoolZeroAlloc(true);

            // Assert
            var result = _builder.Build();
            Assert.That(result.CSharpType, Is.EqualTo(typeof(bool)));
            Assert.That(result.ValueCount, Is.EqualTo(1));
            Assert.That(result.Width, Is.EqualTo(5)); // "True"
        }

        [Test]
        public void ProcessString_ValidInteger_ParsesAsInt()
        {
            // Act
            _builder.ProcessString("42".AsSpan());

            // Assert
            var result = _builder.Build();
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.ValueCount, Is.EqualTo(1));
        }

        [Test]
        public void ProcessString_WithUnicode_SetsUnicodeFlag()
        {
            // Act
            _builder.ProcessString("héllo".AsSpan());

            // Assert
            var result = _builder.Build();
            Assert.That(result.RequiresUnicode, Is.True);
        }

        [Test]
        public void Process_WithNull_IncrementsNullCount()
        {
            // Act
            _builder.Process(null);

            // Assert
            var result = _builder.Build();
            Assert.That(result.NullCount, Is.EqualTo(1));
            Assert.That(result.ValueCount, Is.EqualTo(0));
        }

        [Test]
        public void ProcessMixedTyping_IntAfterString_ThrowsMixedTypingException()
        {
            // Arrange
            _builder.ProcessString("test");

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => _builder.Process(42));
            Assert.That(ex.Message, Does.Contain("Cannot process hard-typed"));
        }

        [Test]
        public void ProcessMixedTyping_StringAfterInt_ThrowsMixedTypingException()
        {
            // Arrange
            _builder.Process(42);

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => _builder.ProcessString("test".AsSpan()));
            Assert.That(ex.Message, Does.Contain("Cannot process string values"));
        }

        [Test]
        public void SetInitialSize_SetsWidthDecimalSizeAndUnicode()
        {
            // Act
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(string), 10, new DecimalSize(2, 3)) { Unicode = true });

            // Assert
            var result = _builder.Build();
            Assert.Multiple(() =>
            {
                Assert.That(result.Width, Is.EqualTo(10));
                Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(2));
                Assert.That(result.DigitsAfterDecimal, Is.EqualTo(3));
                Assert.That(result.RequiresUnicode, Is.True);
            });
        }

        [Test]
        public void SetInitialSize_IsNotAValueSeen()
        {
            // Act
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(decimal), 11, new DecimalSize(5, 5)) { Unicode = true });

            // Assert - the type and the counts are unchanged...
            var result = _builder.Build();
            Assert.Multiple(() =>
            {
                Assert.That(result.CSharpType, Is.EqualTo(typeof(bool))); // First in preference order
                Assert.That(result.ValueCount, Is.EqualTo(0));
                Assert.That(result.NullCount, Is.EqualTo(0));
            });

            // ...and so are the mixed typing checks: a hard typed value is still accepted as the first value
            Assert.DoesNotThrow(() => _builder.Process(42));
            Assert.That(_builder.Build().ValueCount, Is.EqualTo(1));
        }

        [Test]
        public void SetInitialSize_ValuesAreCountedAsWithoutIt()
        {
            // Arrange
            var unprimed = new PooledBuilder(CultureInfo.InvariantCulture);
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(string), 10) { Unicode = true });

            // Act
            foreach (var builder in new[] { _builder, unprimed })
            {
                builder.Process(null);
                builder.Process(DBNull.Value);
                builder.Process("abc");
                builder.Process("   ");
                builder.Process("12");
            }

            // Assert
            var primed = _builder.Build();
            var expected = unprimed.Build();
            Assert.Multiple(() =>
            {
                Assert.That(primed.ValueCount, Is.EqualTo(expected.ValueCount));
                Assert.That(primed.NullCount, Is.EqualTo(expected.NullCount));
                Assert.That(primed.CSharpType, Is.EqualTo(expected.CSharpType));
            });
        }

        [Test]
        public void SetInitialSize_NegativeWidth_IsIgnored()
        {
            // Act - e.g. FAnsiSql requests a width of -1 for types that have no string length
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(int), -1));

            // Assert
            Assert.That(_builder.Build().Width, Is.Null);
        }

        [Test]
        public void SetInitialSize_DoesNotShrinkWhatWasAlreadySeen()
        {
            // Arrange
            _builder.ProcessString("abcdefghijklmno".AsSpan());

            // Act
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(string), 10));

            // Assert
            Assert.That(_builder.Build().Width, Is.EqualTo(15));
        }

        [Test]
        public void SetInitialSize_IsClearedByReset()
        {
            // Arrange
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(string), 10, new DecimalSize(5, 5)) { Unicode = true });

            // Act
            _builder.Reset();

            // Assert
            var result = _builder.Build();
            Assert.Multiple(() =>
            {
                Assert.That(result.Width, Is.Null);
                Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(0));
                Assert.That(result.DigitsAfterDecimal, Is.EqualTo(0));
                Assert.That(result.RequiresUnicode, Is.False);
            });
        }

        [Test]
        public void SetInitialSize_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _builder.SetInitialSize(null!));
        }

        [Test]
        public void ProcessIntZeroAlloc_KeepsTheInitialDecimalSize()
        {
            // Arrange - decimal(10,5)
            _builder.SetInitialSize(new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 5)));

            // Act
            _builder.ProcessIntZeroAlloc(7);

            // Assert
            var result = _builder.Build();
            Assert.Multiple(() =>
            {
                Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(5));
                Assert.That(result.DigitsAfterDecimal, Is.EqualTo(5));
            });
        }
    }
}
