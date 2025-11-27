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
            var request2 = new DatabaseTypeRequest(typeof(System.Drawing.Color), 200); // Truly unsupported type

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex.Message, Does.Contain("Could not combine Types"));
        }

        [Test]
        public void PreferenceOrder_ContainsExpectedTypes()
        {
            // Assert - verify the exact sequence of types
            var expectedOrder = new[]
            {
                typeof(bool),
                typeof(int),
                typeof(decimal),
                typeof(long),
                typeof(TimeSpan),
                typeof(DateTime),
                typeof(byte),
                typeof(short),
                typeof(Guid),
                typeof(byte[]),
                typeof(string)
            };

            Assert.That(DatabaseTypeRequest.PreferenceOrder, Is.EqualTo(expectedOrder).AsCollection);
        }

        [Test]
        public void PreferenceOrderIndex_MatchesPreferenceOrder()
        {
            // Assert - verify that PreferenceOrderIndex contains exactly the same types as PreferenceOrder
            Assert.That(DatabaseTypeRequest.PreferenceOrderIndex.Count, Is.EqualTo(DatabaseTypeRequest.PreferenceOrder.Count));

            for (var i = 0; i < DatabaseTypeRequest.PreferenceOrder.Count; i++)
            {
                var type = DatabaseTypeRequest.PreferenceOrder[i];
                Assert.That(DatabaseTypeRequest.PreferenceOrderIndex.ContainsKey(type), Is.True,
                    $"PreferenceOrderIndex missing type: {type.Name}");
                Assert.That(DatabaseTypeRequest.PreferenceOrderIndex[type], Is.EqualTo(i),
                    $"PreferenceOrderIndex has wrong index for type: {type.Name}");
            }
        }

        [Test]
        public void PreferenceOrder_ContainsAllCommonSqlTypes()
        {
            // Assert - verify all common SQL types are represented
            var expectedTypes = new[]
            {
                typeof(bool),      // SQL bit, boolean
                typeof(byte),      // SQL tinyint
                typeof(short),     // SQL smallint
                typeof(int),       // SQL int
                typeof(long),      // SQL bigint
                typeof(decimal),   // SQL decimal, numeric, money
                typeof(TimeSpan),  // SQL time
                typeof(DateTime),  // SQL datetime, datetime2, date, timestamp
                typeof(Guid),      // SQL uniqueidentifier, uuid
                typeof(byte[]),    // SQL varbinary, binary, image, blob
                typeof(string)     // SQL varchar, nvarchar, char, text
            };

            foreach (var expectedType in expectedTypes)
            {
                Assert.That(DatabaseTypeRequest.PreferenceOrder, Does.Contain(expectedType),
                    $"PreferenceOrder missing common SQL type: {expectedType.Name}");
            }
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

        #region Type-Specific Equality Tests (Issue #19)

        [Test]
        public void Equals_Decimal_IgnoresWidth()
        {
            // Arrange - same decimal type and Size, different Width
            var request1 = new DatabaseTypeRequest(typeof(decimal), 100, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 2));

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "Decimals with same Size but different Width should be equal");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()), "Hash codes must match for equal decimals");
        }

        [Test]
        public void Equals_Decimal_IgnoresUnicode()
        {
            // Arrange - same decimal type and Size, different Unicode
            var request1 = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(4, 1)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(4, 1)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "Decimals with same Size but different Unicode should be equal");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()), "Hash codes must match for equal decimals");
        }

        [Test]
        public void Equals_Decimal_ComparesSizeCorrectly()
        {
            // Arrange - same decimal type, different Size
            var request1 = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(5, 2));
            var request2 = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(4, 1));

            // Act & Assert
            Assert.That(request1, Is.Not.EqualTo(request2), "Decimals with different Size should not be equal");
        }

        [Test]
        public void Equals_NullableDecimal_IgnoresWidthAndUnicode()
        {
            // Arrange - nullable decimal
            var request1 = new DatabaseTypeRequest(typeof(decimal?), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(decimal?), null, new DecimalSize(5, 2)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "Nullable decimals with same Size but different Width/Unicode should be equal");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_DateTime_IgnoresAllPropertiesExceptType()
        {
            // Arrange - same DateTime type, different Width, Size, Unicode
            var request1 = new DatabaseTypeRequest(typeof(DateTime), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(DateTime), 200, new DecimalSize(8, 4)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "DateTimes with same type should be equal regardless of Width/Size/Unicode");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_TimeSpan_IgnoresAllPropertiesExceptType()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(TimeSpan), 50, new DecimalSize(3, 1)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(TimeSpan), null, new DecimalSize(0, 0)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2));
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_Int_IgnoresAllPropertiesExceptType()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(int), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(int), null, new DecimalSize(0, 0)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2));
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_Bool_IgnoresAllPropertiesExceptType()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(bool), 1) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(bool), 10) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2));
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_Guid_IgnoresAllPropertiesExceptType()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(Guid), 36) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(Guid), null) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2));
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_ByteArray_ComparesWidthOnly()
        {
            // Arrange - same Width
            var request1 = new DatabaseTypeRequest(typeof(byte[]), 1000, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(byte[]), 1000, new DecimalSize(0, 0)) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "byte[] with same Width should be equal (Size/Unicode ignored)");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_ByteArray_DifferentWidth_NotEqual()
        {
            // Arrange - different Width
            var request1 = new DatabaseTypeRequest(typeof(byte[]), 1000);
            var request2 = new DatabaseTypeRequest(typeof(byte[]), 2000);

            // Act & Assert
            Assert.That(request1, Is.Not.EqualTo(request2), "byte[] with different Width should not be equal");
        }

        [Test]
        public void Equals_String_ComparesWidthAndUnicode()
        {
            // Arrange - same Width and Unicode
            var request1 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(5, 2)) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 100, new DecimalSize(0, 0)) { Unicode = true };

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2), "Strings with same Width/Unicode should be equal (Size ignored)");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_String_DifferentWidth_NotEqual()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 200) { Unicode = true };

            // Act & Assert
            Assert.That(request1, Is.Not.EqualTo(request2), "Strings with different Width should not be equal");
        }

        [Test]
        public void Equals_String_DifferentUnicode_NotEqual()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(string), 100) { Unicode = true };
            var request2 = new DatabaseTypeRequest(typeof(string), 100) { Unicode = false };

            // Act & Assert
            Assert.That(request1, Is.Not.EqualTo(request2), "Strings with different Unicode should not be equal");
        }

        [Test]
        public void Equals_RoundTrip_DecimalFromGuesserAndSQL()
        {
            // This test simulates the scenario from issue #19:
            // Guesser creates a DatabaseTypeRequest with Width set
            // SQL reverse-engineering creates one with Width=null
            // They should be equal if Size matches

            // Arrange - simulate guesser-created (with Width)
            var fromGuesser = new DatabaseTypeRequest(typeof(decimal), 4, new DecimalSize(4, 1));

            // Simulate SQL reverse-engineered (without Width)
            var fromSQL = new DatabaseTypeRequest(typeof(decimal), null, new DecimalSize(4, 1));

            // Act & Assert
            Assert.That(fromSQL, Is.EqualTo(fromGuesser),
                "Round-trip: Decimal from SQL and Guesser should be equal when Size matches");
            Assert.That(fromSQL.GetHashCode(), Is.EqualTo(fromGuesser.GetHashCode()));
        }

        [Test]
        public void Equals_String_WithSameWidthButDifferentSize_AreEqual()
        {
            // This tests the Copilot-identified edge case:
            // Two strings with same _maxWidthForStrings but different Size should be equal
            // because Width property computes Math.Max(_maxWidthForStrings, Size.ToStringLength())

            // Arrange - both have _maxWidthForStrings=10, but different Size values
            var request1 = new DatabaseTypeRequest(typeof(string), 10, new DecimalSize(20, 0));
            var request2 = new DatabaseTypeRequest(typeof(string), 10, new DecimalSize(0, 0));

            // Verify the Width property values are different due to Size
            Assert.That(request1.Width, Is.EqualTo(20), "Width should include Size length");
            Assert.That(request2.Width, Is.EqualTo(10), "Width should be just the explicit value");

            // Act & Assert - they should still be equal because we compare _maxWidthForStrings, not Width
            Assert.That(request1, Is.EqualTo(request2),
                "Strings with same _maxWidthForStrings should be equal even if Size differs");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        [Test]
        public void Equals_ByteArray_WithSameWidthButDifferentSize_AreEqual()
        {
            // This tests the Copilot-identified edge case for byte arrays:
            // Two byte[] with same _maxWidthForStrings but different Size should be equal

            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(byte[]), 1000, new DecimalSize(20, 0));
            var request2 = new DatabaseTypeRequest(typeof(byte[]), 1000, new DecimalSize(0, 0));

            // Verify the Width property values are the same (1000) despite different Size values,
            // because Width returns Math.Max(_maxWidthForStrings, Size.ToStringLength())
            Assert.That(request1.Width, Is.EqualTo(1000), "Width should be max of explicit and Size");
            Assert.That(request2.Width, Is.EqualTo(1000), "Width should be just the explicit value");

            // Act & Assert
            Assert.That(request1, Is.EqualTo(request2),
                "byte[] with same _maxWidthForStrings should be equal even if Size differs");
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
        }

        #endregion
    }
}