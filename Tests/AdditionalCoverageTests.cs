using System;
using System.Data;
using System.Globalization;
using NUnit.Framework;

namespace TypeGuesser.Tests
{
    [TestFixture]
    public class AdditionalCoverageTests
    {
        [Test]
        public void GetSharedFactory_WithVariousCultures_ReturnsCorrectFactories()
        {
            // Test different cultures to cover the factory caching logic
            var cultures = new[]
            {
                CultureInfo.InvariantCulture,
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR"),
                new CultureInfo("de-DE"),
                new CultureInfo("ja-JP")
            };

            foreach (var culture in cultures)
            {
                var factory = Guesser.GetSharedFactory(culture);
                Assert.That(factory, Is.Not.Null);
                Assert.That(factory.Settings, Is.Not.Null);
            }
        }

        [Test]
        public void GetSharedFactory_CachingBehavior_ReturnsSameInstances()
        {
            // Test that the same culture returns the same factory instance
            var culture = new CultureInfo("es-ES");
            var factory1 = Guesser.GetSharedFactory(culture);
            var factory2 = Guesser.GetSharedFactory(culture);
            var factory3 = Guesser.GetSharedFactory(culture);

            Assert.That(factory1, Is.SameAs(factory2));
            Assert.That(factory2, Is.SameAs(factory3));
        }

        [Test]
        public void DatabaseTypeRequest_Max_WithIncompatibleTypes_ThrowsNotSupportedException()
        {
            // Test the resource string path in DatabaseTypeRequest.Max
            var request1 = new DatabaseTypeRequest(typeof(Guid));
            var request2 = new DatabaseTypeRequest(typeof(Type));

            var ex = Assert.Throws<NotSupportedException>(() => DatabaseTypeRequest.Max(request1, request2));
            Assert.That(ex.Message, Does.Contain("Could not combine Types"));
        }

        [Test]
        public void Guesser_Dispose_MultipleCalls_Safe()
        {
            // Test that multiple dispose calls are safe
            var guesser = new Guesser();
            guesser.AdjustToCompensateForValue("test");

            guesser.Dispose();
            guesser.Dispose(); // Should not throw
            guesser.Dispose(); // Should not throw

            Assert.Pass("Multiple dispose calls completed successfully");
        }

        [Test]
        public void Guesser_DisposedAccess_ThrowsObjectDisposedException()
        {
            // Test accessing methods after dispose
            var guesser = new Guesser();
            guesser.AdjustToCompensateForValue("test");
            guesser.Dispose();

            Assert.Throws<ObjectDisposedException>(() => guesser.AdjustToCompensateForValue("123"));
        }

        [Test]
        public void Guesser_Parse_WithVariousTypes()
        {
            // Test the Parse method with different guess types
            using var guesser = new Guesser();

            // Test int parsing
            guesser.AdjustToCompensateForValue(42);
            var intResult = guesser.Parse("123");
            Assert.That(intResult, Is.EqualTo(123));

            // Reset for next test
            guesser.Dispose();
            var newGuesser = new Guesser();

            // Test decimal parsing
            newGuesser.AdjustToCompensateForValue(12.34m);
            var decimalResult = newGuesser.Parse("56.78");
            Assert.That(decimalResult, Is.EqualTo(56.78m));

            newGuesser.Dispose();
        }

        [Test]
        public void Guesser_ShouldDowngradeColumnType_Logic()
        {
            // Test the ShouldDowngradeColumnTypeToMatchCurrentEstimate method
            using var guesser = new Guesser();
            guesser.AdjustToCompensateForValue(42); // int type

            var table = new DataTable();
            var stringColumn = table.Columns.Add("StringCol", typeof(string));
            var objectColumn = table.Columns.Add("ObjectCol", typeof(object));
            var intColumn = table.Columns.Add("IntCol", typeof(int));

            // Should downgrade string and object columns to int
            Assert.That(guesser.ShouldDowngradeColumnTypeToMatchCurrentEstimate(stringColumn), Is.True);
            Assert.That(guesser.ShouldDowngradeColumnTypeToMatchCurrentEstimate(objectColumn), Is.True);

            // Should not change explicitly typed int column
            Assert.That(guesser.ShouldDowngradeColumnTypeToMatchCurrentEstimate(intColumn), Is.False);
        }

        [Test]
        public void Guesser_ExtraLengthPerNonAsciiCharacter_Property()
        {
            // Test the ExtraLengthPerNonAsciiCharacter property
            const int extraLength = 3;
            var guesser = new Guesser { ExtraLengthPerNonAsciiCharacter = extraLength };

            Assert.That(guesser.ExtraLengthPerNonAsciiCharacter, Is.EqualTo(extraLength));

            guesser.Dispose();
        }

        [Test]
        public void DecimalTypeDecider_ParseErrors_ThrowFormatException()
        {
            // Test error handling in DecimalTypeDecider
            var decider = new TypeGuesser.Deciders.DecimalTypeDecider(CultureInfo.InvariantCulture);

            var invalidInputs = new[]
            {
                "not_a_number",
                "12.34.56",
                "abc123",
                "12e",
                "1e+abc"
            };

            foreach (var input in invalidInputs)
            {
                Assert.Throws<FormatException>(() => decider.Parse(input.AsSpan()));
            }
        }

        [Test]
        public void DateTimeTypeDecider_ParseErrors_ThrowFormatException()
        {
            // Test error handling in DateTimeTypeDecider
            var decider = new TypeGuesser.Deciders.DateTimeTypeDecider(CultureInfo.InvariantCulture);

            var invalidInputs = new[]
            {
                "not_a_date",
                "32/01/2023",
                "13/13/2023",
                "2023-99-99",
                "invalid_date_format"
            };

            foreach (var input in invalidInputs)
            {
                Assert.Throws<FormatException>(() => decider.Parse(input.AsSpan()));
            }
        }

        [Test]
        public void BoolTypeDecider_ParseErrors_ThrowFormatException()
        {
            // Test error handling in BoolTypeDecider
            var decider = new TypeGuesser.Deciders.BoolTypeDecider(CultureInfo.InvariantCulture);

            var invalidInputs = new[]
            {
                "maybe",
                "unknown",
                "2",
                "onoff",
                "yesno"
            };

            foreach (var input in invalidInputs)
            {
                Assert.Throws<FormatException>(() => decider.Parse(input.AsSpan()));
            }
        }

        [Test]
        public void IntTypeDecider_ParseErrors_ThrowFormatException()
        {
            // Test error handling in IntTypeDecider
            var decider = new TypeGuesser.Deciders.IntTypeDecider(CultureInfo.InvariantCulture);

            var invalidInputs = new[]
            {
                "12.34",
                "not_a_number",
                "999999999999999999999999999999",
                "1e100"
            };

            foreach (var input in invalidInputs)
            {
                Assert.Throws<FormatException>(() => decider.Parse(input.AsSpan()));
            }
        }

        [Test]
        public void MixedTyping_VariousScenarios_ThrowMixedTypingException()
        {
            // Test various mixed typing scenarios
            var scenarios = new[]
            {
                (object)"string", (object)42,
                (object)true, (object)"test",
                (object)12.34m, (object)false,
                (object)DateTime.Now, (object)"date"
            };

            for (int i = 0; i < scenarios.Length; i += 2)
            {
                using var guesser = new Guesser();
                guesser.AdjustToCompensateForValue(scenarios[i]);

                Assert.Throws<MixedTypingException>(() =>
                    guesser.AdjustToCompensateForValue(scenarios[i + 1]));
            }
        }

        [Test]
        public void TypeDeciderFactory_UnsupportedTypes_ThrowException()
        {
            // Test TypeDeciderFactory with unsupported types
            var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
            var unsupportedTypes = new[]
            {
                typeof(AdditionalCoverageTests),
                typeof(Exception),
                typeof(AdditionalCoverageTests),
                typeof(Action),
                typeof(Func<int>)
            };

            foreach (var type in unsupportedTypes)
            {
                Assert.Catch<Exception>(() => factory.Create(type));
            }
        }

        [Test]
        public void Constructor_WithDatabaseTypeRequest_HandlesVariousTypes()
        {
            // Test Guesser constructor with different DatabaseTypeRequest types
            var requests = new[]
            {
                new DatabaseTypeRequest(typeof(int)),
                new DatabaseTypeRequest(typeof(decimal)),
                new DatabaseTypeRequest(typeof(string)),
                new DatabaseTypeRequest(typeof(DateTime))
            };

            foreach (var request in requests)
            {
                using var guesser = new Guesser(request);
                Assert.That(guesser.Guess.CSharpType, Is.EqualTo(request.CSharpType));
            }
        }
    }
}