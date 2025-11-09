using NUnit.Framework;
using System;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class DatabaseTypeRequestEnhancedTests
    {
        [Test]
        public void Constructor_WithType_SetsPropertiesCorrectly()
        {
            // Act
            var request = new DatabaseTypeRequest(typeof(int));

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(request.Width, Is.Null);
            Assert.That(request.Size, Is.Not.Null);
            Assert.That(request.Unicode, Is.False);
        }

        [Test]
        public void Constructor_WithTypeAndWidth_SetsPropertiesCorrectly()
        {
            // Act
            var request = new DatabaseTypeRequest(typeof(string), 100);

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(request.Width, Is.EqualTo(100));
            Assert.That(request.Size, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithTypeWidthAndDecimalSize_SetsPropertiesCorrectly()
        {
            // Arrange
            var decimalSize = new DecimalSize(8, 4);

            // Act
            var request = new DatabaseTypeRequest(typeof(decimal), 50, decimalSize);

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(request.Width, Is.EqualTo(50));
            Assert.That(request.Size, Is.EqualTo(decimalSize));
        }

        [Test]
        public void Constructor_WithNullDecimalSize_UsesDefault()
        {
            // Act
            var request = new DatabaseTypeRequest(typeof(decimal), 50, null);

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(request.Width, Is.EqualTo(50));
            Assert.That(request.Size, Is.Not.Null);
            Assert.That(request.Size.NumbersBeforeDecimalPlace, Is.EqualTo(0));
            Assert.That(request.Size.NumbersAfterDecimalPlace, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_CreatesStringType()
        {
            // Act
            var request = new DatabaseTypeRequest();

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(request.Width, Is.Null);
            Assert.That(request.Unicode, Is.False);
        }

        [Test]
        public void Width_WhenSetToNull_ReturnsSizeToStringLength()
        {
            // Arrange
            var request = new DatabaseTypeRequest(typeof(decimal));
            request.Size = new DecimalSize(5, 2);

            // Act
            var width = request.Width;

            // Assert
            Assert.That(width, Is.EqualTo(8)); // 5 + 2 + 1 for decimal point
        }

        [Test]
        public void Width_WhenSetToValueHigherThanSize_ReturnsSetValue()
        {
            // Arrange
            var request = new DatabaseTypeRequest(typeof(decimal));
            request.Size = new DecimalSize(5, 2);
            request.Width = 100;

            // Act
            var width = request.Width;

            // Assert
            Assert.That(width, Is.EqualTo(100));
        }

        [Test]
        public void Width_WhenSetToValueLowerThanSize_ReturnsSizeLength()
        {
            // Arrange
            var request = new DatabaseTypeRequest(typeof(decimal));
            request.Size = new DecimalSize(5, 2);
            request.Width = 5; // Lower than the string representation length

            // Act
            var width = request.Width;

            // Assert
            Assert.That(width, Is.EqualTo(8)); // Should return the larger value
        }

        [Test]
        public void Equals_WithSameProperties_ReturnsTrue()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2)) { Unicode = true };

            // Act & Assert
            Assert.That(request1.Equals(request2), Is.True);
        }

        [Test]
        public void Equals_WithDifferentTypes_ReturnsFalse()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100);
            var request2 = new DatabaseTypeRequest(typeof(int), 100);

            // Act & Assert
            Assert.That(request1.Equals(request2), Is.False);
        }

        [Test]
        public void Equals_WithDifferentWidth_ReturnsFalse()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100);
            var request2 = new DatabaseTypeRequest(typeof(string), 200);

            // Act & Assert
            Assert.That(request1.Equals(request2), Is.False);
        }

        [Test]
        public void Equals_WithDifferentUnicode_ReturnsFalse()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 100) { Unicode = false };

            // Act & Assert
            Assert.That(request1.Equals(request2), Is.False);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var request = new DatabaseTypeRequest(typeof(string));

            // Act & Assert
            Assert.That(request, Is.Not.Null);
            Assert.That(request.Equals((object?)null), Is.False);
        }

        [Test]
        public void Equals_WithSameReference_ReturnsTrue()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string));
            var request2 = request1; // Same reference

            // Act & Assert
            Assert.That(request1.Equals(request2), Is.True);
            Assert.That(ReferenceEquals(request1, request2), Is.True);
        }

        [Test]
        public void GetHashCode_WithSameProperties_ReturnsSameHashCode()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2)) { Unicode = true };

            // Act
            var hash1 = request1.GetHashCode();
            var hash2 = request2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void EqualityOperator_WithEqualRequests_ReturnsTrue()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100);
            var request2 = new DatabaseTypeRequest(typeof(string), 100);

            // Act & Assert
            Assert.That(request1 == request2, Is.True);
        }

        [Test]
        public void InequalityOperator_WithEqualRequests_ReturnsFalse()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100);
            var request2 = new DatabaseTypeRequest(typeof(string), 100);

            // Act & Assert
            Assert.That(request1 != request2, Is.False);
        }

        [Test]
        public void EqualityOperator_WithDifferentRequests_ReturnsFalse()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100);
            var request2 = new DatabaseTypeRequest(typeof(int), 100);

            // Act & Assert
            Assert.That(request1 == request2, Is.False);
        }

        [Test]
        public void Max_WithHigherPreferenceType_ReturnsHigherPreference()
        {
            // Arrange
            var intRequest = new DatabaseTypeRequest(typeof(int));
            var stringRequest = new DatabaseTypeRequest(typeof(string)) { Unicode = true };

            // Act
            var result = DatabaseTypeRequest.Max(intRequest, stringRequest);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.Unicode, Is.True); // Should combine unicode flags
        }

        [Test]
        public void Max_WithLowerPreferenceType_ReturnsHigherPreference()
        {
            // Arrange
            var intRequest = new DatabaseTypeRequest(typeof(int)) { Unicode = true };
            var stringRequest = new DatabaseTypeRequest(typeof(string));

            // Act
            var result = DatabaseTypeRequest.Max(intRequest, stringRequest);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.Unicode, Is.True);
        }

        [Test]
        public void Max_WithSameType_CombinesProperties()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(string), 200, new DecimalSize(8, 4)) { Unicode = true };

            // Act
            var result = DatabaseTypeRequest.Max(request1, request2);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(result.Width, Is.EqualTo(200)); // Max width
            Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(8)); // Max precision
            Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(4)); // Max scale
            Assert.That(result.Unicode, Is.True); // Combined unicode flag
        }

        [Test]
        public void Max_WithSameTypeAndNullWidths_UsesNonNullWidth()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), null, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(string), 200, new DecimalSize(8, 4));

            // Act
            var result = DatabaseTypeRequest.Max(request1, request2);

            // Assert
            Assert.That(result.Width, Is.EqualTo(200));
        }

        [Test]
        public void Max_WithBothNullWidths_ReturnsNull()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), null, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(string), null, new DecimalSize(8, 4));

            // Act
            var result = DatabaseTypeRequest.Max(request1, request2);

            // Assert
            Assert.That(result.Width, Is.Null);
        }

        [Test]
        public void Max_WithUnsupportedType_ThrowsNotSupportedException()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(DateTime), 100);
            var request2 = new DatabaseTypeRequest(typeof(Guid), 200);

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex.Message, Does.Contain("Could not combine Types"));
        }

        [Test]
        public void PreferenceOrder_ContainsExpectedTypes()
        {
            // Assert
            Assert.That(DatabaseTypeRequest.PreferenceOrder.Count, Is.EqualTo(6));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[0], Is.EqualTo(typeof(bool)));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[1], Is.EqualTo(typeof(int)));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[2], Is.EqualTo(typeof(decimal)));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[3], Is.EqualTo(typeof(TimeSpan)));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[4], Is.EqualTo(typeof(DateTime)));
            Assert.That(DatabaseTypeRequest.PreferenceOrder[5], Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Max_WithSameTypeAndDifferentDecimalSizes_CombinesCorrectly()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(decimal), 100, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(decimal), 150, new DecimalSize(8, 1));

            // Act
            var result = DatabaseTypeRequest.Max(request1, request2);

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(result.Width, Is.EqualTo(150));
            Assert.That(result.Size.NumbersBeforeDecimalPlace, Is.EqualTo(8)); // Max
            Assert.That(result.Size.NumbersAfterDecimalPlace, Is.EqualTo(2)); // Max
        }

        [Test]
        public void Max_ReturnsOriginalInstance_WhenAlreadyLargeEnough()
        {
            // Arrange
            var largeRequest = new DatabaseTypeRequest(typeof(string), 200, new DecimalSize(10, 5));
            var smallRequest = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2));

            // Act
            var result = DatabaseTypeRequest.Max(largeRequest, smallRequest);

            // Assert
            Assert.That(result, Is.SameAs(largeRequest));
        }
    }
}