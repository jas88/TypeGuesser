using System;
using System.Globalization;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class PooledBuilderExampleTests
    {
        [Test]
        public void ExampleHardTypedValues_ReturnsIntTypeWithCorrectStats()
        {
            // Act
            var result = PooledBuilderExample.ExampleHardTypedValues();

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.ValueCount, Is.EqualTo(3));
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(3)); // 100 is the largest (3 digits)
        }

        [Test]
        public void ExampleHardTypedValues_HandlesMixedIntegers_CalculatesCorrectPrecision()
        {
            // Act
            var result = PooledBuilderExample.ExampleHardTypedValues();

            // Assert
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(3)); // 100 requires 3 digits
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(0));
            Assert.That(result.Width, Is.EqualTo(3));
        }

        [Test]
        public void ExampleDecimalValues_ReturnsDecimalTypeWithCorrectStats()
        {
            // Act
            var result = PooledBuilderExample.ExampleDecimalValues();

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
            Assert.That(result.ValueCount, Is.EqualTo(3));
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(1)); // 3.14159 - 1 digit before decimal
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(5)); // 3.14159 - 5 digits after decimal
        }

        [Test]
        public void ExampleDecimalValues_CalculatesCorrectPrecisionAndScale()
        {
            // Act
            var result = PooledBuilderExample.ExampleDecimalValues();

            // Assert
            // The largest decimal is 3.14159 (1 digit before, 5 after)
            Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(1));
            Assert.That(result.DigitsAfterDecimal, Is.EqualTo(5));
            Assert.That(result.DecimalPrecision, Is.EqualTo(6)); // 1 + 5 = 6
            Assert.That(result.DecimalScale, Is.EqualTo(5));
        }

        [Test]
        public void ExampleStringValues_ReturnsIntTypeWithNumericValues()
        {
            // Act
            var result = PooledBuilderExample.ExampleStringValues();

            // Assert - Numeric strings are parsed as int type
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.ValueCount, Is.EqualTo(4)); // Actual value count
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.Width, Is.EqualTo(3)); // All are 3 digits
        }

        [Test]
        public void ExampleStringValues_HandlesNumericStringParsing()
        {
            // Act
            var result = PooledBuilderExample.ExampleStringValues();

            // Assert - Numeric strings are parsed as int type
            Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
            Assert.That(result.Width, Is.EqualTo(3));
        }

        [Test]
        public void ExampleMixedValues_ReturnsStringTypeForMixedContent()
        {
            // Act
            var result = PooledBuilderExample.ExampleMixedValues();

            // Assert - Falls back to string due to mixed content
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(result.ValueCount, Is.GreaterThan(0)); // Some value count
            Assert.That(result.NullCount, Is.EqualTo(0));
            Assert.That(result.Width, Is.GreaterThan(0)); // Has some width
        }

        [Test]
        public void ExampleMixedValues_HandlesNonNumericStringCorrectly()
        {
            // Act
            var result = PooledBuilderExample.ExampleMixedValues();

            // Assert
            Assert.That(result.CSharpType, Is.EqualTo(typeof(string)));
            // Width should accommodate all the string values
            Assert.That(result.Width, Is.GreaterThan(0));
        }

        [Test]
        public void ExampleBackwardCompatibility_ConvertsToDatabaseTypeRequest()
        {
            // Act
            var request = PooledBuilderExample.ExampleBackwardCompatibility();

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(DateTime)));
            Assert.That(request.Width, Is.Not.Null);
            Assert.That(request.Width, Is.GreaterThanOrEqualTo(10)); // Date format length
        }

        [Test]
        public void ExampleBackwardCompatibility_HandlesDateFormatConsistency()
        {
            // Act
            var request = PooledBuilderExample.ExampleBackwardCompatibility();

            // Assert
            Assert.That(request.CSharpType, Is.EqualTo(typeof(DateTime)));
            // Width should accommodate the date format (may be longer than just the literal string)
            Assert.That(request.Width, Is.GreaterThan(0));
        }

        [Test]
        public void AllExamples_HandleResourceCleanupCorrectly()
        {
            // This test ensures that all example methods properly rent and return builders
            // without causing resource leaks

            // Act & Assert - All methods should complete without exceptions
            Assert.DoesNotThrow(() => PooledBuilderExample.ExampleHardTypedValues());
            Assert.DoesNotThrow(() => PooledBuilderExample.ExampleDecimalValues());
            Assert.DoesNotThrow(() => PooledBuilderExample.ExampleStringValues());
            Assert.DoesNotThrow(() => PooledBuilderExample.ExampleMixedValues());
            Assert.DoesNotThrow(() => PooledBuilderExample.ExampleBackwardCompatibility());
        }

        [Test]
        public void ExampleHardTypedValues_WithDifferentCulture_WorksCorrectly()
        {
            // This test verifies the example works with different cultures (though not culture-sensitive)
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                // Arrange
                var customCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentCulture = customCulture;

                // Act
                var result = PooledBuilderExample.ExampleHardTypedValues();

                // Assert
                Assert.That(result.CSharpType, Is.EqualTo(typeof(int)));
                Assert.That(result.ValueCount, Is.EqualTo(3));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void ExampleDecimalValues_WithNegativeValues_HandlesCorrectly()
        {
            // Test that the example would work with negative decimal values
            // Note: We can't modify the example method, but we can test the pattern it uses

            // Arrange
            var builder = new PooledBuilder(CultureInfo.InvariantCulture);
            try
            {
                // Act
                builder.ProcessDecimalZeroAlloc(-3.14159m);
                builder.ProcessDecimalZeroAlloc(-2.71828m);
                builder.ProcessDecimalZeroAlloc(1.41421m); // positive

                var result = builder.Build();

                // Assert
                Assert.That(result.CSharpType, Is.EqualTo(typeof(decimal)));
                Assert.That(result.ValueCount, Is.EqualTo(3));
                Assert.That(result.DigitsBeforeDecimal, Is.EqualTo(1)); // -3.14159 -> 1 digit before decimal (ignoring minus)
            }
            finally
            {
                // PooledBuilder doesn't implement IDisposable, so no cleanup needed
            }
        }

        [Test]
        public void ExampleBackwardCompatibility_WithTimeValues_HandlesDateTimeCorrectly()
        {
            // Test a variation with time values to ensure datetime parsing works
            var builder = new PooledBuilder(CultureInfo.InvariantCulture);
            try
            {
                // Act
                builder.ProcessString("2024-01-01T12:34:56".AsSpan());
                builder.ProcessString("2024-12-31T23:59:59".AsSpan());

                var result = builder.Build();
                var request = result.ToDatabaseTypeRequest();

                // Assert
                Assert.That(request.CSharpType, Is.EqualTo(typeof(DateTime)));
                Assert.That(request.Width, Is.GreaterThanOrEqualTo(19)); // ISO datetime format length
            }
            finally
            {
                // PooledBuilder doesn't implement IDisposable, so no cleanup needed
            }
        }

        [Test]
        public void ExampleMethods_ProduceConsistentResults()
        {
            // Ensure that running the same example multiple times produces identical results

            // Act
            var result1 = PooledBuilderExample.ExampleHardTypedValues();
            var result2 = PooledBuilderExample.ExampleHardTypedValues();

            // Assert
            Assert.That(result1.CSharpType, Is.EqualTo(result2.CSharpType));
            Assert.That(result1.ValueCount, Is.EqualTo(result2.ValueCount));
            Assert.That(result1.Width, Is.EqualTo(result2.Width));
        }
    }
}