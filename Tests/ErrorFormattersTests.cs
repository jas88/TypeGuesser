using System;
using NUnit.Framework;
using TypeGuesser.Deciders;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class ErrorFormattersTests
    {
        [Test]
        public void UnsupportedType_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.UnsupportedType(typeof(int));

            // Assert
            Assert.That(message, Is.EqualTo("No Type Decider exists for Type:System.Int32"));
        }

        [Test]
        public void CannotCombineTypes_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.CannotCombineTypes(typeof(int), typeof(string));

            // Assert
            Assert.That(message, Is.EqualTo("Could not combine Types 'System.Int32' and 'System.String' because they were of differing Types and neither Type appeared in the PreferenceOrder"));
        }

        [Test]
        public void DateTimeParseError_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.DateTimeParseError("invalid_date");

            // Assert
            Assert.That(message, Is.EqualTo("Could not parse 'invalid_date' to a valid DateTime"));
        }

        [Test]
        public void StringParseError_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.StringParseError("123abc", typeof(IntTypeDecider));

            // Assert
            Assert.That(message, Is.EqualTo("Could not parse string value '123abc' with Decider Type:IntTypeDecider"));
        }

        [Test]
        public void MixedTypingError_ReturnsCorrectMessage()
        {
            // Arrange
            var value = 42;
            var valueType = typeof(int);
            var previousType = typeof(string);

            // Act
            var message = ErrorFormatters.MixedTypingError(value, valueType, previousType);

            // Assert
            Assert.That(message, Is.EqualTo("Guesser does not support being passed hard typed objects (e.g. int) mixed with untyped objects (e.g. string).  We were adjusting to compensate for object '42' which is of Type 'System.Int32', we were previously passed a 'System.String' type"));
        }

        [Test]
        public void MixedTypingIntAfterString_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.MixedTypingIntAfterString();

            // Assert
            Assert.That(message, Is.EqualTo("Cannot process hard-typed int value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects."));
        }

        [Test]
        public void MixedTypingDecimalAfterString_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.MixedTypingDecimalAfterString();

            // Assert
            Assert.That(message, Is.EqualTo("Cannot process hard-typed decimal value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects."));
        }

        [Test]
        public void MixedTypingBoolAfterString_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.MixedTypingBoolAfterString();

            // Assert
            Assert.That(message, Is.EqualTo("Cannot process hard-typed bool value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects."));
        }

        [Test]
        public void MixedTypingGenericTypeAfterString_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.MixedTypingGenericTypeAfterString(typeof(DateTime));

            // Assert
            Assert.That(message, Is.EqualTo("Cannot process hard-typed System.DateTime value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects."));
        }

        [Test]
        public void AbstractBaseError_ReturnsCorrectMessage()
        {
            // Act
            var message = ErrorFormatters.AbstractBaseError();

            // Assert
            Assert.That(message, Is.EqualTo("DecideTypesForStrings abstract base was not passed any typesSupported by implementing derived class"));
        }
    }
}