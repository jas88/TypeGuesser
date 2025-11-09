using System;
using System.Globalization;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class PooledBuilderTests
    {
        private PooledBuilder _builder;

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
            var builder = new PooledBuilder(null);

            // Assert
            Assert.That(builder.Culture, Is.EqualTo(CultureInfo.CurrentCulture));
        }

        [Test]
        public void Reset_RestoresInitialState()
        {
            // Arrange
            _builder.ProcessIntZeroAlloc(42);
            _builder.ProcessString("test");

            // Act
            _builder.Reset();

            // Assert
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
            _builder.SetCulture(null);

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
    }
}