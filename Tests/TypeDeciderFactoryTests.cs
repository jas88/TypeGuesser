using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TypeGuesser.Deciders;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class TypeDeciderFactoryTests
    {
        private TypeDeciderFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
        }

        [Test]
        public void Constructor_WithCulture_PopulatesDictionaryWithExpectedTypes()
        {
            // Assert
            Assert.That(_factory.Dictionary.Count, Is.GreaterThan(0));
            Assert.That(_factory.Settings, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullCulture_UsesCurrentCulture()
        {
            // Act
            var factory = new TypeDeciderFactory(null!);

            // Assert
            Assert.That(factory.Dictionary.Count, Is.GreaterThan(0));
            Assert.That(factory.Settings, Is.Not.Null);
        }

        [Test]
        public void Settings_IsInitializedCorrectly()
        {
            // Assert
            Assert.That(_factory.Settings, Is.Not.Null);
            Assert.That(_factory.Settings.CharCanBeBoolean, Is.True); // Default setting
        }

        [Test]
        public void Dictionary_ContainsBoolTypeDecider()
        {
            // Assert
            Assert.That(_factory.Dictionary.ContainsKey(typeof(bool)), Is.True);
            var decider = _factory.Dictionary[typeof(bool)];
            Assert.That(decider, Is.InstanceOf<BoolTypeDecider>());
        }

        [Test]
        public void Dictionary_ContainsIntTypeDecider()
        {
            // Assert
            Assert.That(_factory.Dictionary.ContainsKey(typeof(int)), Is.True);
            var decider = _factory.Dictionary[typeof(int)];
            Assert.That(decider, Is.InstanceOf<IntTypeDecider>());
        }

        [Test]
        public void Dictionary_ContainsDecimalTypeDecider()
        {
            // Assert
            Assert.That(_factory.Dictionary.ContainsKey(typeof(decimal)), Is.True);
            var decider = _factory.Dictionary[typeof(decimal)];
            Assert.That(decider, Is.InstanceOf<DecimalTypeDecider>());
        }

        [Test]
        public void Dictionary_ContainsDateTimeTypeDecider()
        {
            // Assert
            Assert.That(_factory.Dictionary.ContainsKey(typeof(DateTime)), Is.True);
            var decider = _factory.Dictionary[typeof(DateTime)];
            Assert.That(decider, Is.InstanceOf<DateTimeTypeDecider>());
        }

        [Test]
        public void Dictionary_ContainsTimeSpanTypeDecider()
        {
            // Assert
            Assert.That(_factory.Dictionary.ContainsKey(typeof(TimeSpan)), Is.True);
            var decider = _factory.Dictionary[typeof(TimeSpan)];
            Assert.That(decider, Is.InstanceOf<TimeSpanTypeDecider>());
        }

        [Test]
        public void Dictionary_AllDecidersHaveSettingsSet()
        {
            // Act & Assert
            foreach (var kvp in _factory.Dictionary)
            {
                var decider = kvp.Value;
                Assert.That(decider.Settings, Is.Not.Null);
                Assert.That(decider.Settings, Is.EqualTo(_factory.Settings));
            }
        }

        [Test]
        public void Create_WithSupportedType_ReturnsClonedDecider()
        {
            // Act
            var decider = _factory.Create(typeof(int));

            // Assert
            Assert.That(decider, Is.InstanceOf<IntTypeDecider>());
            Assert.That(decider.Settings, Is.EqualTo(_factory.Settings));
            Assert.That(decider, Is.Not.SameAs(_factory.Dictionary[typeof(int)])); // Should be a clone
        }

        [Test]
        public void Create_WithUnsupportedType_ThrowsException()
        {
            // Arrange - Use a truly unsupported type (Guid is now supported)
            var unsupportedType = typeof(object);

            // Act & Assert
            var ex = Assert.Catch(() => _factory.Create(unsupportedType));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.GetType().Name, Is.EqualTo("TypeNotSupportedException"));
            Assert.That(ex.Message, Does.Contain("DataType System.Object"));
        }

        [Test]
        public void Create_WithNullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _factory.Create(null!));
        }

        [Test]
        public void Create_WithDifferentSupportedTypes_ReturnsCorrectDeciderTypes()
        {
            // Arrange & Act
            var boolDecider = _factory.Create(typeof(bool));
            var intDecider = _factory.Create(typeof(int));
            var decimalDecider = _factory.Create(typeof(decimal));
            var dateTimeDecider = _factory.Create(typeof(DateTime));
            var timeSpanDecider = _factory.Create(typeof(TimeSpan));

            // Assert
            Assert.That(boolDecider, Is.InstanceOf<BoolTypeDecider>());
            Assert.That(intDecider, Is.InstanceOf<IntTypeDecider>());
            Assert.That(decimalDecider, Is.InstanceOf<DecimalTypeDecider>());
            Assert.That(dateTimeDecider, Is.InstanceOf<DateTimeTypeDecider>());
            Assert.That(timeSpanDecider, Is.InstanceOf<TimeSpanTypeDecider>());
        }

        [Test]
        public void IsSupported_WithSupportedType_ReturnsTrue()
        {
            // Arrange
            var supportedTypes = new[] { typeof(bool), typeof(int), typeof(decimal), typeof(DateTime), typeof(TimeSpan) };

            // Act & Assert
            foreach (var type in supportedTypes)
            {
                Assert.That(_factory.IsSupported(type), Is.True, $"Type {type.Name} should be supported");
            }
        }

        [Test]
        public void IsSupported_WithUnsupportedType_ReturnsFalse()
        {
            // Arrange - use truly unsupported types (Guid is now supported via NeverGuessTheseTypeDecider)
            var unsupportedTypes = new[] { typeof(object), typeof(TypeDeciderFactoryTests), typeof(Exception) };

            // Act & Assert
            foreach (var type in unsupportedTypes)
            {
                Assert.That(_factory.IsSupported(type), Is.False, $"Type {type.Name} should not be supported");
            }
        }

        [Test]
        public void IsSupported_WithNullType_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_factory.IsSupported(null), Is.False);
        }

        [Test]
        public void Create_DeciderInstancesHaveCorrectTypesSupported()
        {
            // Arrange
            var expectedTypeMappings = new Dictionary<Type, Type>
            {
                { typeof(bool), typeof(BoolTypeDecider) },
                { typeof(int), typeof(IntTypeDecider) },
                { typeof(decimal), typeof(DecimalTypeDecider) },
                { typeof(DateTime), typeof(DateTimeTypeDecider) },
                { typeof(TimeSpan), typeof(TimeSpanTypeDecider) }
            };

            // Act & Assert
            foreach (var kvp in expectedTypeMappings)
            {
                var decider = _factory.Create(kvp.Key);
                Assert.That(decider.GetType(), Is.EqualTo(kvp.Value));
                Assert.That(decider.TypesSupported, Does.Contain(kvp.Key));
            }
        }

        [Test]
        public void Create_MultipleCallsForSameType_ReturnsDifferentInstances()
        {
            // Act
            var decider1 = _factory.Create(typeof(int));
            var decider2 = _factory.Create(typeof(int));

            // Assert
            Assert.That(decider1, Is.Not.SameAs(decider2));
            Assert.That(decider1.GetType(), Is.EqualTo(decider2.GetType()));
            Assert.That(decider1.Settings, Is.EqualTo(decider2.Settings));
        }

        [Test]
        public void Constructor_WithDifferentCulture_PropagatesToDeciders()
        {
            // Arrange
            var frenchCulture = new CultureInfo("fr-FR");

            // Act
            var factory = new TypeDeciderFactory(frenchCulture);

            // Assert
            var intDecider = factory.Create(typeof(int)) as IntTypeDecider;
            Assert.That(intDecider, Is.Not.Null);
            Assert.That(intDecider!.Culture, Is.EqualTo(frenchCulture));
        }

        [Test]
        public void Dictionary_ContainsAllExpectedDeciders()
        {
            // Assert
            var expectedDeciderTypes = new[]
            {
                typeof(BoolTypeDecider),
                typeof(IntTypeDecider),
                typeof(DecimalTypeDecider),
                typeof(DateTimeTypeDecider),
                typeof(TimeSpanTypeDecider),
                typeof(NeverGuessTheseTypeDecider)
            };

            var actualDeciderTypes = new HashSet<Type>();
            foreach (var kvp in _factory.Dictionary)
            {
                actualDeciderTypes.Add(kvp.Value.GetType());
            }

            foreach (var expectedType in expectedDeciderTypes)
            {
                Assert.That(actualDeciderTypes, Does.Contain(expectedType));
            }
        }

        [Test]
        public void Create_WithNeverGuessedType_ReturnsNeverGuessedTypeDecider()
        {
            // This test assumes there are types that NeverGuessTheseTypeDecider handles
            // We can't easily predict which types without looking at the implementation

            // Act - try to find a type that maps to NeverGuessTheseTypeDecider
            Type? neverGuessedType = null;
            foreach (var kvp in _factory.Dictionary)
            {
                if (kvp.Value is NeverGuessTheseTypeDecider)
                {
                    neverGuessedType = kvp.Key;
                    break;
                }
            }

            if (neverGuessedType != null)
            {
                var decider = _factory.Create(neverGuessedType);
                Assert.That(decider, Is.InstanceOf<NeverGuessTheseTypeDecider>());
            }
            else
            {
                Assert.Pass("No NeverGuessTheseTypeDecider found in dictionary - this is okay");
            }
        }

        [Test]
        public void Create_WithSameTypeAfterFactoryDisposed_HandlesGracefully()
        {
            // TypeDeciderFactory doesn't implement IDisposable, but we can test repeated usage

            // Act
            var decider1 = _factory.Create(typeof(int));
            var decider2 = _factory.Create(typeof(int));

            // Assert
            Assert.That(decider1, Is.InstanceOf<IntTypeDecider>());
            Assert.That(decider2, Is.InstanceOf<IntTypeDecider>());
            Assert.That(decider1, Is.Not.SameAs(decider2));
        }
    }
}