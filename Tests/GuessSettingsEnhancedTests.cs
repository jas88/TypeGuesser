using System;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class GuessSettingsEnhancedTests
    {
        [Test]
        public void PublicConstructor_CreatesInstanceWithDefaults()
        {
            // Act
            var settings = new GuessSettings();

            // Assert
            Assert.That(settings.CharCanBeBoolean, Is.True);
            Assert.That(settings.ExplicitDateFormats, Is.Null);
        }

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new GuessSettings();
            original.CharCanBeBoolean = false;
            original.ExplicitDateFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy" };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.That(cloned.CharCanBeBoolean, Is.EqualTo(original.CharCanBeBoolean));
            Assert.That(cloned.ExplicitDateFormats, Is.EqualTo(original.ExplicitDateFormats));

            // Verify independence - modify original
            original.CharCanBeBoolean = true;
            original.ExplicitDateFormats = new[] { "MM/dd/yyyy" };

            // Clone should not be affected
            Assert.That(cloned.CharCanBeBoolean, Is.False);
            Assert.That(cloned.ExplicitDateFormats, Is.EqualTo(new[] { "yyyy-MM-dd", "dd/MM/yyyy" }));
        }

        [Test]
        public void Clone_PreservesAllProperties()
        {
            // Arrange
            var original = new GuessSettings();
            original.CharCanBeBoolean = true;
            original.ExplicitDateFormats = new[] { "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy" };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.That(cloned.CharCanBeBoolean, Is.True);
            Assert.That(cloned.ExplicitDateFormats, Is.EqualTo(original.ExplicitDateFormats));
        }

        [Test]
        public void Clone_WithNullExplicitDateFormats_HandlesCorrectly()
        {
            // Arrange
            var original = new GuessSettings();
            original.CharCanBeBoolean = false;
            original.ExplicitDateFormats = null;

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.That(cloned.CharCanBeBoolean, Is.False);
            Assert.That(cloned.ExplicitDateFormats, Is.Null);
        }

        [Test]
        public void CopyTo_CopiesAllPropertiesToTarget()
        {
            // Arrange
            var source = new GuessSettings();
            source.CharCanBeBoolean = false;
            source.ExplicitDateFormats = new[] { "yyyy-MM-dd", "MM/dd/yyyy" };

            var target = new GuessSettings();
            target.CharCanBeBoolean = true;
            target.ExplicitDateFormats = new[] { "dd/MM/yyyy" };

            // Act
            source.CopyTo(target);

            // Assert
            Assert.That(target.CharCanBeBoolean, Is.EqualTo(source.CharCanBeBoolean));
            Assert.That(target.ExplicitDateFormats, Is.EqualTo(source.ExplicitDateFormats));
        }

        [Test]
        public void CopyTo_WithNullExplicitDateFormats_SetsNullCorrectly()
        {
            // Arrange
            var source = new GuessSettings();
            source.CharCanBeBoolean = true;
            source.ExplicitDateFormats = null;

            var target = new GuessSettings();
            target.CharCanBeBoolean = false;
            target.ExplicitDateFormats = new[] { "yyyy-MM-dd" };

            // Act
            source.CopyTo(target);

            // Assert
            Assert.That(target.CharCanBeBoolean, Is.True);
            Assert.That(target.ExplicitDateFormats, Is.Null);
        }

        [Test]
        public void CopyTo_DoesNotAffectSourceInstance()
        {
            // Arrange
            var source = new GuessSettings();
            source.CharCanBeBoolean = true;
            source.ExplicitDateFormats = new[] { "yyyy-MM-dd" };

            var target = new GuessSettings();

            // Act
            source.CopyTo(target);

            // Modify target after copy
            target.CharCanBeBoolean = false;
            target.ExplicitDateFormats = new[] { "MM/dd/yyyy" };

            // Assert
            Assert.That(source.CharCanBeBoolean, Is.True);
            Assert.That(source.ExplicitDateFormats, Is.EqualTo(new[] { "yyyy-MM-dd" }));
        }

        [Test]
        public void CopyTo_WithEmptyExplicitDateFormats_HandlesCorrectly()
        {
            // Arrange
            var source = new GuessSettings();
            source.CharCanBeBoolean = false;
            source.ExplicitDateFormats = Array.Empty<string>();

            var target = new GuessSettings();

            // Act
            source.CopyTo(target);

            // Assert
            Assert.That(target.CharCanBeBoolean, Is.False);
            Assert.That(target.ExplicitDateFormats, Is.EqualTo(Array.Empty<string>()));
        }

        [Test]
        public void CopyTo_MultipleCalls_OverwritesCorrectly()
        {
            // Arrange
            var source1 = new GuessSettings();
            source1.CharCanBeBoolean = true;
            source1.ExplicitDateFormats = new[] { "yyyy-MM-dd" };

            var source2 = new GuessSettings();
            source2.CharCanBeBoolean = false;
            source2.ExplicitDateFormats = new[] { "dd/MM/yyyy" };

            var target = new GuessSettings();

            // Act
            source1.CopyTo(target);
            source2.CopyTo(target);

            // Assert - Second copy should overwrite first
            Assert.That(target.CharCanBeBoolean, Is.False);
            Assert.That(target.ExplicitDateFormats, Is.EqualTo(new[] { "dd/MM/yyyy" }));
        }

        [Test]
        public void CharCanBeBoolean_DefaultValue_IsTrue()
        {
            // Arrange & Act
            var settings = new GuessSettings();

            // Assert
            Assert.That(settings.CharCanBeBoolean, Is.True);
        }

        [Test]
        public void CharCanBeBoolean_CanBeSetToFalse()
        {
            // Arrange
            var settings = new GuessSettings();

            // Act
            settings.CharCanBeBoolean = false;

            // Assert
            Assert.That(settings.CharCanBeBoolean, Is.False);
        }

        [Test]
        public void ExplicitDateFormats_DefaultValue_IsNull()
        {
            // Arrange & Act
            var settings = new GuessSettings();

            // Assert
            Assert.That(settings.ExplicitDateFormats, Is.Null);
        }

        [Test]
        public void ExplicitDateFormats_CanBeSetToValidFormats()
        {
            // Arrange
            var settings = new GuessSettings();
            var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };

            // Act
            settings.ExplicitDateFormats = formats;

            // Assert
            Assert.That(settings.ExplicitDateFormats, Is.EqualTo(formats));
        }

        [Test]
        public void ExplicitDateFormats_CanBeSetToEmptyArray()
        {
            // Arrange
            var settings = new GuessSettings();

            // Act
            settings.ExplicitDateFormats = Array.Empty<string>();

            // Assert
            Assert.That(settings.ExplicitDateFormats, Is.EqualTo(Array.Empty<string>()));
        }

        [Test]
        public void CloneAndCopyTo_ProduceSameResults()
        {
            // Arrange
            var original = new GuessSettings();
            original.CharCanBeBoolean = true;
            original.ExplicitDateFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy" };

            // Act
            var cloned = original.Clone();
            var copyTarget = new GuessSettings();
            original.CopyTo(copyTarget);

            // Assert
            Assert.That(cloned.CharCanBeBoolean, Is.EqualTo(copyTarget.CharCanBeBoolean));
            Assert.That(cloned.ExplicitDateFormats, Is.EqualTo(copyTarget.ExplicitDateFormats));
        }

        [Test]
        public void GuessSettings_AreSerializable()
        {
            // Arrange
            var original = new GuessSettings();
            original.CharCanBeBoolean = false;
            original.ExplicitDateFormats = new[] { "yyyy-MM-dd HH:mm:ss" };

            // Act
            var serialized = System.Text.Json.JsonSerializer.Serialize(original);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<GuessSettings>(serialized);

            // Assert
            Assert.That(deserialized!.CharCanBeBoolean, Is.EqualTo(original.CharCanBeBoolean));
            Assert.That(deserialized.ExplicitDateFormats, Is.EqualTo(original.ExplicitDateFormats));
        }

        [Test]
        public void GuessSettings_EqualityComparison()
        {
            // Arrange
            var settings1 = new GuessSettings();
            var settings2 = new GuessSettings();
            settings2.CharCanBeBoolean = settings1.CharCanBeBoolean;
            settings2.ExplicitDateFormats = settings1.ExplicitDateFormats;

            // Act & Assert
            Assert.That(settings1.CharCanBeBoolean, Is.EqualTo(settings2.CharCanBeBoolean));
            Assert.That(settings1.ExplicitDateFormats, Is.EqualTo(settings2.ExplicitDateFormats));
        }
    }
}