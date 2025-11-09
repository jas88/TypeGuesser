using NUnit.Framework;
using System;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class TypeGuessResultTests
    {
        [Test]
        public void Constructor_WithNullType_UsesStringAsDefault()
        {
            // Act
            var result = new TypeGuessResult(null);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Constructor_WithValidType_SetsTypeCorrectly()
        {
            // Act
            var result = new TypeGuessResult(typeof(int));

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void Constructor_WithNegativeDigits_ClampsToZero()
        {
            // Act
            var result = new TypeGuessResult(
                typeof(decimal),
                width: 10,
                digitsBeforeDecimal: -5,
                digitsAfterDecimal: -3);

            // Assert
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(0));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPositiveDigits_SetsValuesCorrectly()
        {
            // Act
            var result = new TypeGuessResult(
                typeof(decimal),
                width: 10,
                digitsBeforeDecimal: 5,
                digitsAfterDecimal: 2);

            // Assert
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(5));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_SetsAllPropertiesCorrectly()
        {
            // Act
            var result = new TypeGuessResult(
                typeof(string),
                width: 100,
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4,
                requiresUnicode: true,
                valueCount: 50,
                nullCount: 5);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(result.Width, Is.EqualTo(100));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(8));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(4));
            Assert.That(result.RequiresUnicode, Is.True);
            Assert.That(result.ValueCount, Is.EqualTo(50));
            Assert.That(result.NullCount, Is.EqualTo(5));
        }

        [Test]
        public void DecimalPrecision_ReturnsSumOfDigits()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4);

            // Act & Assert
            Assert.That(result.DecimalPrecision, Is.EqualTo(12));
        }

        [Test]
        public void DecimalScale_ReturnsDigitsAfterDecimal()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4);

            // Act & Assert
            Assert.That(result.DecimalScale, Is.EqualTo(4));
        }

        [Test]
        public void IsDecimalSizeEmpty_WithZeroDigits_ReturnsTrue()
        {
            // Arrange
            var result = new TypeGuessResult(typeof(int));

            // Act & Assert
            Assert.That(result.IsDecimalSizeEmpty, Is.True);
        }

        [Test]
        public void IsDecimalSizeEmpty_WithNonZeroDigits_ReturnsFalse()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                digitsBeforeDecimal: 5,
                digitsAfterDecimal: 2);

            // Act & Assert
            Assert.That(result.IsDecimalSizeEmpty, Is.False);
        }

        [Test]
        public void ToDatabaseTypeRequest_ConvertsCorrectly()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                width: 50,
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4,
                requiresUnicode: true,
                valueCount: 100,
                nullCount: 10);

            // Act
            var request = result.ToDatabaseTypeRequest();

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(request.Width, Is.EqualTo(50));
            Assert.That(request.Size.NumbersBeforeDecimalPlace, Is.EqualTo(8));
            Assert.That(request.Size.NumbersAfterDecimalPlace, Is.EqualTo(4));
            Assert.That(request.Unicode, Is.True);
        }

        [Test]
        public void FromDatabaseTypeRequest_ConvertsCorrectly()
        {
            // Arrange
            var decimalSize = new DecimalSize(8, 4);
            var request = new DatabaseTypeRequest(typeof(decimal), 50, decimalSize)
            {
                Unicode = true
            };

            // Act
            var result = TypeGuessResult.FromDatabaseTypeRequest(request, valueCount: 100, nullCount: 10);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(result.Width, Is.EqualTo(50));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(8));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(4));
            Assert.That(result.RequiresUnicode, Is.True);
            Assert.That(result.ValueCount, Is.EqualTo(100));
            Assert.That(result.NullCount, Is.EqualTo(10));
        }

        [Test]
        public void FromDatabaseTypeRequest_WithDefaultCounts_SetsZeroCounts()
        {
            // Arrange
            var request = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize());

            // Act
            var result = TypeGuessResult.FromDatabaseTypeRequest(request);

            // Assert
            Assert.That(result.ValueCount, Is.EqualTo(0));
            Assert.That(result.NullCount, Is.EqualTo(0));
        }

        [Test]
        public void ToString_WithTypeOnly_ReturnsTypeName()
        {
            // Arrange
            var result = new TypeGuessResult(typeof(int));

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("Int32 [0 values, 0 nulls]"));
        }

        [Test]
        public void ToString_WithWidth_ReturnsWidth()
        {
            // Arrange
            var result = new TypeGuessResult(typeof(string), width: 100);

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("String(100) [0 values, 0 nulls]"));
        }

        [Test]
        public void ToString_WithDecimalPrecision_ReturnsPrecisionAndScale()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4);

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("Decimal(12,4) [0 values, 0 nulls]"));
        }

        [Test]
        public void ToString_WithUnicode_ReturnsUnicodeIndicator()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(string),
                requiresUnicode: true);

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("String unicode [0 values, 0 nulls]"));
        }

        [Test]
        public void ToString_WithAllProperties_ReturnsCompleteString()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(decimal),
                width: 50,
                digitsBeforeDecimal: 8,
                digitsAfterDecimal: 4,
                requiresUnicode: true,
                valueCount: 100,
                nullCount: 10);

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("Decimal(50)(12,4) unicode [100 values, 10 nulls]"));
        }

        [Test]
        public void ToString_WithValuesAndNulls_ReturnsCorrectCounts()
        {
            // Arrange
            var result = new TypeGuessResult(
                typeof(int),
                valueCount: 42,
                nullCount: 8);

            // Act
            var str = result.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("Int32 [42 values, 8 nulls]"));
        }

        [Test]
        public void Constructor_WithAllDefaultValues_CreatesValidResult()
        {
            // Act
            var result = new TypeGuessResult(typeof(string));

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(result.Width, Is.Null);
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(0));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(0));
            Assert.That(result.RequiresUnicode, Is.False);
            Assert.That(result.ValueCount, Is.EqualTo(0));
            Assert.That(result.NullCount, Is.EqualTo(0));
        }
    }
}