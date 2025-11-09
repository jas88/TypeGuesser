using System;
using System.Data;
using System.Globalization;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class ResourceStringTests
    {
        [Test]
        public void DatabaseTypeRequest_Max_WithUnsupportedTypes_ThrowsResourceException()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(Guid));
            var request2 = new DatabaseTypeRequest(typeof(DateTimeOffset));

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex.Message, Does.Contain("Could not combine Types"));
            Assert.That(ex.Message, Does.Contain("because they were of differing Types and neither Type appeared in the PreferenceOrder"));
        }

        [Test]
        public void DateTimeTypeDecider_Parse_WithInvalidFormat_ThrowsResourceException()
        {
            // This test triggers the DateTime parsing error message

            // Arrange
            var culture = CultureInfo.InvariantCulture;
            var decider = new TypeGuesser.Deciders.DateTimeTypeDecider(culture);

            // Act & Assert
            // We need to find a way to trigger the parsing error
            var ex = Assert.Throws<FormatException>(() =>
            {
                // Try to parse an invalid date format that will trigger the error
                decider.Parse("definitely_not_a_date_12345_invalid".AsSpan());
            });

            // The exception message should contain the resource string
            Assert.That(ex.Message, Does.Contain("Could not parse"));
            Assert.That(ex.Message, Does.Contain("to a valid DateTime"));
        }

        [Test]
        public void TypeDeciderFactory_Create_WithUnsupportedType_ThrowsResourceException()
        {
            // Arrange
            var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
            var unsupportedType = typeof(ResourceStringTests); // Use this test class as unsupported type

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => factory.Create(unsupportedType));
            Assert.That(ex.Message, Does.Contain("does not have an associated IDecideTypesForStrings"));
        }

        [Test]
        public void Guesser_MixedTypeHandling_ThrowsResourceException()
        {
            // Arrange
            using var guesser = new Guesser();

            // Act - Mix hard-typed and untyped values
            guesser.AdjustToCompensateForValue("string_value"); // String first

            // Assert - Adding int after string should throw with resource message
            var ex = Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue(42));
            Assert.That(ex.Message, Does.Contain("Guesser does not support being passed hard typed objects"));
            Assert.That(ex.Message, Does.Contain("mixed with untyped objects"));
        }

        [Test]
        public void Guesser_MixedTypeHandling_ReverseOrder_ThrowsResourceException()
        {
            // Arrange
            using var guesser = new Guesser();

            // Act - Mix untyped and hard-typed values
            guesser.AdjustToCompensateForValue(42); // Int first

            // Assert - Adding string after int should throw with resource message
            var ex = Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue("string_value"));
            Assert.That(ex.Message, Does.Contain("Guesser does not support being passed hard typed objects"));
            Assert.That(ex.Message, Does.Contain("mixed with untyped objects"));
        }

        [Test]
        public void ResourceString_VerifyAllErrorMessages()
        {
            // This test ensures all resource strings are accessible and formatted correctly

            // Test 1: DatabaseTypeRequest.Max error
            var request1 = new DatabaseTypeRequest(typeof(Guid));
            var request2 = new DatabaseTypeRequest(typeof(TimeSpan));
            var ex1 = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex1.Message, Does.Contain("Could not combine Types 'System.Guid' and 'System.TimeSpan'"));

            // Test 2: TypeDeciderFactory error
            var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
            var ex2 = Assert.Throws<Exception>(() => factory.Create(typeof(ResourceStringTests)));
            Assert.That(ex2.Message, Does.Contain("DataType TypeGuesser.Tests.ResourceStringTests does not have an associated IDecideTypesForStrings"));

            // Test 3: Mixed typing error
            using var guesser = new Guesser();
            guesser.AdjustToCompensateForValue("test");
            var ex3 = Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue(123));
            Assert.That(ex3.Message, Does.Contain("we were previously passed a 'System.String' type"));
        }

        [Test]
        public void ResourceString_DeciderErrorMessages()
        {
            // Test various decider error scenarios that use resource strings

            // Test Decimal decider with invalid input
            var decimalDecider = new TypeGuesser.Deciders.DecimalTypeDecider(CultureInfo.InvariantCulture);

            var ex = Assert.Throws<FormatException>(() =>
            {
                decimalDecider.Parse("not_a_decimal_at_all_123_invalid".AsSpan());
            });
            Assert.That(ex.Message, Does.Contain("Could not parse string value"));
            Assert.That(ex.Message, Does.Contain("with Decider Type:DecimalTypeDecider"));
        }

        [Test]
        public void ResourceString_CultureSpecificErrors()
        {
            // Test that resource strings work correctly with different cultures

            var frenchCulture = new CultureInfo("fr-FR");
            var factory = new TypeDeciderFactory(frenchCulture);

            // The error message should still be in English regardless of factory culture
            var ex = Assert.Throws<Exception>(() => factory.Create(typeof(ResourceStringTests)));
            Assert.That(ex.Message, Does.Contain("DataType TypeGuesser.Tests.ResourceStringTests does not have an associated IDecideTypesForStrings"));
        }

        [Test]
        public void ResourceString_ParseErrorDetails()
        {
            // Test detailed parse error messages with specific problematic inputs

            var intDecider = new TypeGuesser.Deciders.IntTypeDecider(CultureInfo.InvariantCulture);

            // Test with various problematic inputs
            var problematicInputs = new[]
            {
                "123abc456",
                "12.34.56",
                "1e10invalid",
                "number_with_text_123"
            };

            foreach (var input in problematicInputs)
            {
                var ex = Assert.Throws<FormatException>(() =>
                {
                    intDecider.Parse(input.AsSpan());
                });

                Assert.That(ex.Message, Does.Contain("Could not parse string value"));
                Assert.That(ex.Message, Does.Contain($"'{input}'"));
                Assert.That(ex.Message, Does.Contain("with Decider Type:IntTypeDecider"));
            }
        }

        [Test]
        public void ResourceString_DatabaseTypeRequestDetailedError()
        {
            // Test DatabaseTypeRequest.Max with specific type combinations that should fail

            // Test with types that aren't in the preference order
            var typeCombinations = new[]
            {
                (typeof(Guid), typeof(DateTimeOffset)),
                (typeof(Type), typeof(IntPtr)),
                (typeof(ResourceStringTests), typeof(ArgumentException))
            };

            foreach (var (type1, type2) in typeCombinations)
            {
                var request1 = new DatabaseTypeRequest(type1);
                var request2 = new DatabaseTypeRequest(type2);

                var ex = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));

                Assert.That(ex.Message, Does.Contain($"Could not combine Types '{type1}' and '{type2}'"));
                Assert.That(ex.Message, Does.Contain("because they were of differing Types and neither Type appeared in the PreferenceOrder"));
            }
        }

        [Test]
        public void ResourceString_ErrorMessageConsistency()
        {
            // Verify that error messages are consistent and contain expected information

            using var guesser = new Guesser();

            // Mix types to trigger error
            guesser.AdjustToCompensateForValue("initial_string");

            var ex = Assert.Throws<MixedTypingException>(() => guesser.AdjustToCompensateForValue(42));

            // Verify error message contains all expected components
            var message = ex.Message;
            Assert.That(message, Does.Contain("Guesser does not support being passed hard typed objects"));
            Assert.That(message, Does.Contain("mixed with untyped objects"));
            Assert.That(message, Does.Contain("which is of Type 'System.Int32'"));
            Assert.That(message, Does.Contain("we were previously passed a 'System.String' type"));
        }
    }
}