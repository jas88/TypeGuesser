using System;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class DecimalSizeEnhancedTests
    {
        [Test]
        public void DefaultConstructor_CreatesEmptySize()
        {
            // Act
            var size = new DecimalSize();

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(0));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(0));
            Assert.That(size.IsEmpty, Is.True);
            Assert.That(size.Precision, Is.EqualTo(0));
            Assert.That(size.Scale, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPositiveValues_SetsValuesCorrectly()
        {
            // Act
            var size = new DecimalSize(5, 3);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(3));
            Assert.That(size.IsEmpty, Is.False);
            Assert.That(size.Precision, Is.EqualTo(8));
            Assert.That(size.Scale, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithNegativeValues_ClampsToZero()
        {
            // Act
            var size = new DecimalSize(-5, -3);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(0));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(0));
            Assert.That(size.IsEmpty, Is.True);
        }

        [Test]
        public void Constructor_WithMixedValues_ClampsNegativeToZero()
        {
            // Act
            var size = new DecimalSize(5, -3);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(0));
            Assert.That(size.Precision, Is.EqualTo(5));
            Assert.That(size.Scale, Is.EqualTo(0));
        }

        [Test]
        public void IsEmpty_WithZeroValues_ReturnsTrue()
        {
            // Arrange
            var size = new DecimalSize(0, 0);

            // Act & Assert
            Assert.That(size.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_WithPositiveBeforeDecimal_ReturnsFalse()
        {
            // Arrange
            var size = new DecimalSize(5, 0);

            // Act & Assert
            Assert.That(size.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_WithPositiveAfterDecimal_ReturnsFalse()
        {
            // Arrange
            var size = new DecimalSize(0, 3);

            // Act & Assert
            Assert.That(size.IsEmpty, Is.False);
        }

        [Test]
        public void Precision_ReturnsSumOfBothValues()
        {
            // Arrange
            var size = new DecimalSize(8, 4);

            // Act & Assert
            Assert.That(size.Precision, Is.EqualTo(12));
        }

        [Test]
        public void Scale_ReturnsAfterDecimalPlace()
        {
            // Arrange
            var size = new DecimalSize(8, 4);

            // Act & Assert
            Assert.That(size.Scale, Is.EqualTo(4));
        }

        [Test]
        public void IncreaseTo_WithSingleParameter_UpdatesOnlyBeforeDecimal()
        {
            // Arrange
            var size = new DecimalSize(3, 2);

            // Act
            size.IncreaseTo(5);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(2));
        }

        [Test]
        public void IncreaseTo_WithSmallerValue_DoesNotChange()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act
            size.IncreaseTo(3, 1);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(3));
        }

        [Test]
        public void IncreaseTo_WithLargerValue_UpdatesBoth()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act
            size.IncreaseTo(8, 6);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(8));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(6));
        }

        [Test]
        public void IncreaseTo_WithMixedValues_UpdatesAppropriately()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act
            size.IncreaseTo(3, 6); // smaller before, larger after

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5)); // unchanged
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(6));  // increased
        }

        [Test]
        public void ToStringLength_WithEmptySize_ReturnsZero()
        {
            // Arrange
            var size = new DecimalSize(0, 0);

            // Act
            var length = size.ToStringLength();

            // Assert
            Assert.That(length, Is.EqualTo(0));
        }

        [Test]
        public void ToStringLength_WithIntegerOnly_ReturnsDigitsCount()
        {
            // Arrange
            var size = new DecimalSize(5, 0);

            // Act
            var length = size.ToStringLength();

            // Assert
            Assert.That(length, Is.EqualTo(5));
        }

        [Test]
        public void ToStringLength_WithDecimal_ReturnsDigitsPlusDecimalPoint()
        {
            // Arrange
            var size = new DecimalSize(3, 2);

            // Act
            var length = size.ToStringLength();

            // Assert
            Assert.That(length, Is.EqualTo(6)); // 3 + 2 + 1 for decimal point
        }

        [Test]
        public void ToStringLength_WithOnlyDecimalPlaces_ReturnsDigitsPlusDecimalPoint()
        {
            // Arrange
            var size = new DecimalSize(0, 3);

            // Act
            var length = size.ToStringLength();

            // Assert
            Assert.That(length, Is.EqualTo(4)); // 0 + 3 + 1 for decimal point
        }

        [Test]
        public void Combine_WithBothNull_ReturnsNull()
        {
            // Act
            var result = DecimalSize.Combine(null, null);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Combine_WithFirstNull_ReturnsSecond()
        {
            // Arrange
            var second = new DecimalSize(5, 3);

            // Act
            var result = DecimalSize.Combine(null, second);

            // Assert
            Assert.That(result, Is.SameAs(second));
        }

        [Test]
        public void Combine_WithSecondNull_ReturnsFirst()
        {
            // Arrange
            var first = new DecimalSize(5, 3);

            // Act
            var result = DecimalSize.Combine(first, null);

            // Assert
            Assert.That(result, Is.SameAs(first));
        }

        [Test]
        public void Combine_WithBothNonNull_ReturnsNewSizeWithMaximumValues()
        {
            // Arrange
            var first = new DecimalSize(5, 2);
            var second = new DecimalSize(3, 6);

            // Act
            var result = DecimalSize.Combine(first, second);

            // Assert
            Assert.That(result.NumbersBeforeDecimalPlace, Is.EqualTo(5)); // max(5,3)
            Assert.That(result.NumbersAfterDecimalPlace, Is.EqualTo(6));  // max(2,6)
            Assert.That(result, Is.Not.SameAs(first));
            Assert.That(result, Is.Not.SameAs(second));
        }

        [Test]
        public void Combine_WithEqualValues_ReturnsCombinedSize()
        {
            // Arrange
            var first = new DecimalSize(5, 3);
            var second = new DecimalSize(5, 3);

            // Act
            var result = DecimalSize.Combine(first, second);

            // Assert
            Assert.That(result.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(result.NumbersAfterDecimalPlace, Is.EqualTo(3));
        }

        [Test]
        public void Equals_WithSameProperties_ReturnsTrue()
        {
            // Arrange
            var size1 = new DecimalSize(5, 3);
            var size2 = new DecimalSize(5, 3);

            // Act & Assert
            Assert.That(size1.Equals(size2), Is.True);
        }

        [Test]
        public void Equals_WithDifferentBeforeDecimal_ReturnsFalse()
        {
            // Arrange
            var size1 = new DecimalSize(5, 3);
            var size2 = new DecimalSize(6, 3);

            // Act & Assert
            Assert.That(size1.Equals(size2), Is.False);
        }

        [Test]
        public void Equals_WithDifferentAfterDecimal_ReturnsFalse()
        {
            // Arrange
            var size1 = new DecimalSize(5, 3);
            var size2 = new DecimalSize(5, 4);

            // Act & Assert
            Assert.That(size1.Equals(size2), Is.False);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act & Assert
            Assert.That(size.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act & Assert
            Assert.That(size.Equals(size), Is.True);
        }

        [Test]
        public void GetHashCode_WithSameProperties_ReturnsSameHashCode()
        {
            // Arrange
            var size1 = new DecimalSize(5, 3);
            var size2 = new DecimalSize(5, 3);

            // Act
            var hash1 = size1.GetHashCode();
            var hash2 = size2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void GetHashCode_WithDifferentProperties_ReturnsDifferentHashCodes()
        {
            // Arrange
            var size1 = new DecimalSize(5, 3);
            var size2 = new DecimalSize(6, 3);

            // Act
            var hash1 = size1.GetHashCode();
            var hash2 = size2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ObjectEquals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act & Assert
            Assert.That(size.Equals("not a DecimalSize"), Is.False);
        }

        [Test]
        public void ToStringLength_WithLargeNumbers_HandlesCorrectly()
        {
            // Arrange
            var size = new DecimalSize(100, 50);

            // Act
            var length = size.ToStringLength();

            // Assert
            Assert.That(length, Is.EqualTo(151)); // 100 + 50 + 1 for decimal point
        }

        [Test]
        public void IncreaseTo_WithZeroValues_DoesNotChangeOriginalValues()
        {
            // Arrange
            var size = new DecimalSize(5, 3);

            // Act
            size.IncreaseTo(0, 0);

            // Assert
            Assert.That(size.NumbersBeforeDecimalPlace, Is.EqualTo(5));
            Assert.That(size.NumbersAfterDecimalPlace, Is.EqualTo(3));
        }

        [Test]
        public void Precision_AndScale_CalculatedCorrectlyAfterIncrease()
        {
            // Arrange
            var size = new DecimalSize(3, 2);

            // Act
            size.IncreaseTo(8, 6);

            // Assert
            Assert.That(size.Precision, Is.EqualTo(14)); // 8 + 6
            Assert.That(size.Scale, Is.EqualTo(6));
        }
    }
}