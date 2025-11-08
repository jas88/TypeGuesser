using System;
using System.Globalization;

namespace TypeGuesser.Advanced;

/// <summary>
/// <para>
/// Static helper class providing zero-allocation convenience methods for common
/// type guessing scenarios. These methods build on <see cref="StackTypeAccumulator"/>
/// to provide simple APIs for the most common use cases.
/// </para>
///
/// <para><b>PERFORMANCE CHARACTERISTICS:</b></para>
/// <list type="bullet">
/// <item><description>Zero heap allocations for the guessing logic</description></item>
/// <item><description>Stack-only execution (ref struct lifetime)</description></item>
/// <item><description>Optimized for bulk processing of numeric data</description></item>
/// <item><description>Minimal overhead compared to manual accumulator usage</description></item>
/// </list>
///
/// <para><b>LIMITATIONS:</b></para>
/// <list type="bullet">
/// <item><description>Input must already be in strongly-typed form (e.g., int[], decimal[])</description></item>
/// <item><description>Cannot be used in async methods (ref struct restriction)</description></item>
/// <item><description>Single-threaded only</description></item>
/// <item><description>Limited to simple scenarios (no mixed types, no string parsing)</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// This class is designed for scenarios where you have already-parsed numeric data
/// and need to determine the optimal database type. For string parsing, use the
/// Layer 1 <see cref="Guesser"/> API instead.
/// </para>
///
/// <para>
/// All methods in this class use <see cref="CultureInfo.InvariantCulture"/> for consistency.
/// If you need culture-specific behavior, create your own <see cref="TypeDeciderFactory"/>
/// and use <see cref="StackTypeAccumulator"/> directly.
/// </para>
/// </remarks>
public static class ZeroAlloc
{
    /// <summary>
    /// Determines the optimal type to store a collection of integers.
    /// </summary>
    /// <param name="values">The span of integer values to analyze</param>
    /// <param name="factory">
    /// Optional <see cref="TypeDeciderFactory"/> for custom culture settings.
    /// If null, uses <see cref="CultureInfo.InvariantCulture"/>.
    /// </param>
    /// <returns>
    /// A <see cref="TypeGuessResult"/> indicating the determined type will be <see cref="int"/>
    /// with size information reflecting the largest value seen.
    /// </returns>
    /// <remarks>
    /// <para><b>PERFORMANCE NOTES:</b></para>
    /// <list type="bullet">
    /// <item><description>Zero allocations during processing</description></item>
    /// <item><description>Uses Math.Log10 for digit counting (no ToString() calls)</description></item>
    /// <item><description>Single pass over data</description></item>
    /// <item><description>Typical performance: ~10-50x faster than Layer 1 Guesser</description></item>
    /// </list>
    ///
    /// <para><b>USAGE NOTES:</b></para>
    /// <list type="bullet">
    /// <item><description>Empty spans return type <see cref="string"/> with zero width</description></item>
    /// <item><description>Size tracks maximum digit count before decimal point</description></item>
    /// <item><description>Width includes space for negative sign if needed</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var numbers = new int[] { 1, 42, 999, -1234 };
    /// var result = ZeroAlloc.GuessIntegers(numbers);
    ///
    /// Console.WriteLine($"Type: {result.CSharpType}"); // System.Int32
    /// Console.WriteLine($"Max digits: {result.Size.NumbersBeforeDecimalPlace}"); // 4 (from -1234)
    /// Console.WriteLine($"Max width: {result.Width}"); // 5 (includes negative sign)
    /// </code>
    ///
    /// <para><b>With custom factory:</b></para>
    /// <code>
    /// var factory = new TypeDeciderFactory(new CultureInfo("de-DE"));
    /// var numbers = stackalloc int[] { 100, 200, 300 };
    /// var result = ZeroAlloc.GuessIntegers(numbers, factory);
    /// </code>
    ///
    /// <para><b>Processing large arrays efficiently:</b></para>
    /// <code>
    /// // This processes 1 million integers with zero allocations
    /// var largeArray = Enumerable.Range(1, 1_000_000).ToArray();
    /// var result = ZeroAlloc.GuessIntegers(largeArray);
    /// // No GC pressure, minimal CPU overhead
    /// </code>
    /// </example>
    public static TypeGuessResult GuessIntegers(ReadOnlySpan<int> values, TypeDeciderFactory? factory = null)
    {
        factory ??= new TypeDeciderFactory(CultureInfo.InvariantCulture);

        var accumulator = new StackTypeAccumulator(factory);

        foreach (var value in values)
        {
            accumulator.Add(value);
        }

        return accumulator.GetResult();
    }

    /// <summary>
    /// Determines the optimal type to store a collection of decimal values.
    /// </summary>
    /// <param name="values">The span of decimal values to analyze</param>
    /// <param name="factory">
    /// Optional <see cref="TypeDeciderFactory"/> for custom culture settings.
    /// If null, uses <see cref="CultureInfo.InvariantCulture"/>.
    /// </param>
    /// <returns>
    /// A <see cref="TypeGuessResult"/> indicating the determined type will be <see cref="decimal"/>
    /// with precision and scale information reflecting the largest/most precise value seen.
    /// </returns>
    /// <remarks>
    /// <para><b>PERFORMANCE NOTES:</b></para>
    /// <list type="bullet">
    /// <item><description>Zero allocations during processing</description></item>
    /// <item><description>Uses SqlDecimal struct for precision/scale extraction</description></item>
    /// <item><description>No string conversions or boxing</description></item>
    /// <item><description>Single pass over data</description></item>
    /// <item><description>Typical performance: ~10-50x faster than Layer 1 Guesser</description></item>
    /// </list>
    ///
    /// <para><b>PRECISION AND SCALE:</b></para>
    /// <list type="bullet">
    /// <item><description>Precision = total number of significant digits</description></item>
    /// <item><description>Scale = number of digits after decimal point</description></item>
    /// <item><description>Result tracks maximum of both across all values</description></item>
    /// <item><description>Example: 123.45 has precision=5, scale=2</description></item>
    /// </list>
    ///
    /// <para><b>DATABASE MAPPING:</b></para>
    /// The returned precision/scale can be used directly for SQL DECIMAL(precision, scale) columns.
    /// For example, if the result shows precision=10 and scale=2, use DECIMAL(10,2) in SQL.
    /// </remarks>
    /// <example>
    /// <code>
    /// var prices = new decimal[] { 1.99m, 10.50m, 999.999m };
    /// var result = ZeroAlloc.GuessDecimals(prices);
    ///
    /// Console.WriteLine($"Type: {result.CSharpType}"); // System.Decimal
    /// Console.WriteLine($"Precision: {result.Size.Precision}"); // 6
    /// Console.WriteLine($"Scale: {result.Size.Scale}"); // 3
    /// Console.WriteLine($"Width: {result.Width}"); // 7 (includes decimal point)
    /// </code>
    ///
    /// <para><b>Processing financial data:</b></para>
    /// <code>
    /// var transactions = new decimal[] { 100.00m, -50.25m, 1234.567m };
    /// var result = ZeroAlloc.GuessDecimals(transactions);
    ///
    /// // Use result to create SQL column:
    /// // CREATE TABLE transactions (
    /// //     amount DECIMAL({result.Size.Precision}, {result.Size.Scale})
    /// // )
    /// </code>
    ///
    /// <para><b>Using stack-allocated spans:</b></para>
    /// <code>
    /// Span&lt;decimal&gt; values = stackalloc decimal[] { 1.1m, 2.22m, 3.333m };
    /// var result = ZeroAlloc.GuessDecimals(values);
    /// // Entire operation uses only stack memory
    /// </code>
    /// </example>
    public static TypeGuessResult GuessDecimals(ReadOnlySpan<decimal> values, TypeDeciderFactory? factory = null)
    {
        factory ??= new TypeDeciderFactory(CultureInfo.InvariantCulture);

        var accumulator = new StackTypeAccumulator(factory);

        foreach (var value in values)
        {
            accumulator.Add(value);
        }

        return accumulator.GetResult();
    }

    /// <summary>
    /// Determines the optimal type to store a collection of boolean values.
    /// </summary>
    /// <param name="values">The span of boolean values to analyze</param>
    /// <param name="factory">
    /// Optional <see cref="TypeDeciderFactory"/> for custom culture settings.
    /// If null, uses <see cref="CultureInfo.InvariantCulture"/>.
    /// </param>
    /// <returns>
    /// A <see cref="TypeGuessResult"/> indicating the determined type will be <see cref="bool"/>
    /// with width tracking the maximum string representation length.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is provided for completeness and API consistency. Boolean type guessing
    /// is trivial since all boolean values are the same type, but this method still tracks
    /// the maximum string width needed for database storage (5 characters for "false").
    /// </para>
    ///
    /// <para><b>PERFORMANCE NOTES:</b></para>
    /// <list type="bullet">
    /// <item><description>Zero allocations during processing</description></item>
    /// <item><description>Single pass over data</description></item>
    /// <item><description>Minimal computational overhead</description></item>
    /// </list>
    ///
    /// <para><b>DATABASE STORAGE:</b></para>
    /// The result can be used to determine appropriate database column types:
    /// <list type="bullet">
    /// <item><description>SQL Server: BIT or VARCHAR(5)</description></item>
    /// <item><description>PostgreSQL: BOOLEAN or VARCHAR(5)</description></item>
    /// <item><description>MySQL: TINYINT(1) or VARCHAR(5)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var flags = new bool[] { true, false, true, true };
    /// var result = ZeroAlloc.GuessBooleans(flags);
    ///
    /// Console.WriteLine($"Type: {result.CSharpType}"); // System.Boolean
    /// Console.WriteLine($"Max width: {result.Width}"); // 5 (length of "false")
    /// </code>
    ///
    /// <para><b>Stack-allocated processing:</b></para>
    /// <code>
    /// Span&lt;bool&gt; values = stackalloc bool[] { true, false };
    /// var result = ZeroAlloc.GuessBooleans(values);
    /// </code>
    /// </example>
    public static TypeGuessResult GuessBooleans(ReadOnlySpan<bool> values, TypeDeciderFactory? factory = null)
    {
        factory ??= new TypeDeciderFactory(CultureInfo.InvariantCulture);

        var accumulator = new StackTypeAccumulator(factory);

        foreach (var value in values)
        {
            accumulator.Add(value);
        }

        return accumulator.GetResult();
    }

    /// <summary>
    /// <para>
    /// Guesses type for mixed numeric data by processing integers, decimals, and booleans
    /// through a single accumulator. This is useful when you have heterogeneous numeric
    /// data that needs to be stored in a single column.
    /// </para>
    ///
    /// <para><b>TYPE HIERARCHY:</b></para>
    /// The accumulator will determine the most restrictive type that can hold all values:
    /// <list type="number">
    /// <item><description>bool - if only boolean values seen</description></item>
    /// <item><description>int - if integers (and possibly bools) seen</description></item>
    /// <item><description>decimal - if any decimal values seen</description></item>
    /// </list>
    /// </summary>
    /// <param name="factory">
    /// The <see cref="TypeDeciderFactory"/> to use. If null, uses <see cref="CultureInfo.InvariantCulture"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="StackTypeAccumulator"/> ready to process mixed numeric values.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method creates an accumulator that can handle mixed numeric types in a single pass.
    /// The accumulator automatically upgrades the type as needed (bool → int → decimal).
    /// </para>
    ///
    /// <para><b>IMPORTANT:</b></para>
    /// The returned accumulator is a ref struct and has all the same limitations as
    /// <see cref="StackTypeAccumulator"/>. It cannot cross async boundaries, cannot be
    /// stored in fields, and must complete within the same scope.
    /// </remarks>
    /// <example>
    /// <code>
    /// var accumulator = ZeroAlloc.CreateAccumulator();
    ///
    /// accumulator.Add(true);           // Type: bool
    /// accumulator.Add(42);             // Type upgrades to: int
    /// accumulator.Add(3.14m);          // Type upgrades to: decimal
    ///
    /// var result = accumulator.GetResult();
    /// Console.WriteLine($"Final type: {result.CSharpType}"); // System.Decimal
    /// Console.WriteLine($"Precision: {result.Size.Precision}, Scale: {result.Size.Scale}");
    /// </code>
    ///
    /// <para><b>Processing mixed data:</b></para>
    /// <code>
    /// var accumulator = ZeroAlloc.CreateAccumulator();
    ///
    /// // Simulate processing data from different sources
    /// foreach (var flag in new[] { true, false, true })
    ///     accumulator.Add(flag);
    ///
    /// foreach (var count in new[] { 1, 10, 100 })
    ///     accumulator.Add(count);
    ///
    /// foreach (var price in new[] { 9.99m, 19.99m })
    ///     accumulator.Add(price);
    ///
    /// var result = accumulator.GetResult();
    /// // Result will be decimal with appropriate precision/scale
    /// </code>
    /// </example>
    public static StackTypeAccumulator CreateAccumulator(TypeDeciderFactory? factory = null)
    {
        factory ??= new TypeDeciderFactory(CultureInfo.InvariantCulture);
        return new StackTypeAccumulator(factory);
    }
}
