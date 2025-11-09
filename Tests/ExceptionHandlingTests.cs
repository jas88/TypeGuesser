using System;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class ExceptionHandlingTests
    {
        [Test]
        public void MixedTypingException_Constructor_WithMessage_SetsMessage()
        {
            // Arrange
            var message = "Mixed typing detected";

            // Act
            var ex = new MixedTypingException(message);

            // Assert
            Assert.That(ex.Message, Is.EqualTo(message));
        }

        [Test]
        public void MixedTypingException_Constructor_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            var message = "Mixed typing detected";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var ex = new MixedTypingException(message, innerException);

            // Assert
            Assert.That(ex.Message, Is.EqualTo(message));
            Assert.That(ex.InnerException, Is.EqualTo(innerException));
        }

  
        [Test]
        public void PooledBuilder_ProcessIntAfterString_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessString("test");

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessIntZeroAlloc(42));
            Assert.That(ex.Message, Does.Contain("Cannot process hard-typed int value after processing string values"));
        }

        [Test]
        public void PooledBuilder_ProcessDecimalAfterString_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessString("test");

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessDecimalZeroAlloc(12.34m));
            Assert.That(ex.Message, Does.Contain("Cannot process hard-typed decimal value after processing string values"));
        }

        [Test]
        public void PooledBuilder_ProcessBoolAfterString_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessString("test");

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessBoolZeroAlloc(true));
            Assert.That(ex.Message, Does.Contain("Cannot process hard-typed bool value after processing string values"));
        }

        [Test]
        public void PooledBuilder_ProcessStringAfterInt_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessIntZeroAlloc(42);

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessString("test".AsSpan()));
            Assert.That(ex.Message, Does.Contain("Cannot process string values after processing hard-typed objects"));
        }

        [Test]
        public void PooledBuilder_ProcessIntAfterDecimal_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessDecimalZeroAlloc(12.34m);

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessIntZeroAlloc(42));
            Assert.That(ex.Message, Does.Contain("Cannot process int value when already primed with type System.Decimal"));
        }

        [Test]
        public void PooledBuilder_ProcessDecimalAfterInt_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessIntZeroAlloc(42);

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessDecimalZeroAlloc(12.34m));
            Assert.That(ex.Message, Does.Contain("Cannot process decimal value when already primed with type System.Int32"));
        }

        [Test]
        public void PooledBuilder_ProcessBoolAfterInt_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessIntZeroAlloc(42);

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.ProcessBoolZeroAlloc(true));
            Assert.That(ex.Message, Does.Contain("Cannot process bool value when already primed with type System.Int32"));
        }

        [Test]
        public void PooledBuilder_ProcessGenericObjectAfterString_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessString("test");
            var customObject = new { Value = 42 };

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.Process(customObject));
            Assert.That(ex.Message, Does.Contain("Cannot process hard-typed"));
        }

        [Test]
        public void PooledBuilder_ProcessGenericObjectOfDifferentType_ThrowsMixedTypingException()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.Process(42);
            var customObject = new { Value = "test" };

            // Act & Assert
            var ex = Assert.Throws<MixedTypingException>(() => builder.Process(customObject));
            Assert.That(ex.Message, Does.Contain("We were adjusting to compensate for object"));
        }

        [Test]
        public void DatabaseTypeRequest_Max_WithUnsupportedTypes_ThrowsNotSupportedException()
        {
            // Arrange
            var request1 = new DatabaseTypeRequest(typeof(Guid));
            var request2 = new DatabaseTypeRequest(typeof(DateTime));

            // Act & Assert
            var ex = Assert.Throws<System.NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex.Message, Does.Contain("Could not combine Types"));
        }

        [Test]
        public void MixedTypingException_IsSerializable()
        {
            // Arrange
            var originalException = new MixedTypingException("Test message");

            // Act
            var serializedException = System.Text.Json.JsonSerializer.Serialize<MixedTypingException>(originalException);
            var deserializedException = System.Text.Json.JsonSerializer.Deserialize<MixedTypingException>(serializedException);

            // Assert
            Assert.That(deserializedException.Message, Is.EqualTo(originalException.Message));
        }

        [Test]
        public void MixedTypingException_InheritsFromException()
        {
            // Arrange & Act
            var ex = new MixedTypingException("Test");

            // Assert
            Assert.That(ex, Is.InstanceOf<Exception>());
        }

    
        [Test]
        public void ExceptionHandling_PooledBuilderReset_ClearsMixedTypingState()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessIntZeroAlloc(42);

            // Reset the builder
            builder.Reset();

            // Act & Assert - Should not throw anymore
            Assert.DoesNotThrow(() => builder.ProcessString("test".AsSpan()));
        }

        [Test]
        public void ExceptionHandling_PooledBuilderMultipleMixedTypingAttempts_AllThrowConsistently()
        {
            // Arrange
            var builder = new PooledBuilder(System.Globalization.CultureInfo.InvariantCulture);
            builder.ProcessIntZeroAlloc(42);

            // Act & Assert
            // Multiple attempts should all throw the same type of exception
            for (int i = 0; i < 3; i++)
            {
                Assert.Throws<MixedTypingException>(() => builder.ProcessString($"test{i}".AsSpan()));
            }
        }
    }
}