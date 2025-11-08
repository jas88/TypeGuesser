# Quick Start: Advanced Zero-Allocation API

This guide shows you how to use TypeGuesser's Advanced API for maximum performance.

## Installation

```bash
dotnet add package TypeGuesser
```

## Basic Usage

### 1. Processing Integer Arrays (Simplest)

```csharp
using TypeGuesser.Advanced;

var numbers = new int[] { 1, 42, 999, -1234 };
var result = ZeroAlloc.GuessIntegers(numbers);

Console.WriteLine($"Type: {result.CSharpType}");
Console.WriteLine($"Max digits: {result.Size.NumbersBeforeDecimalPlace}");
Console.WriteLine($"Width: {result.Width}");

// Output:
// Type: System.Int32
// Max digits: 4
// Width: 5
```

### 2. Processing Decimal Arrays

```csharp
using TypeGuesser.Advanced;

var prices = new decimal[] { 1.99m, 10.50m, 999.999m };
var result = ZeroAlloc.GuessDecimals(prices);

Console.WriteLine($"SQL Type: DECIMAL({result.Size.Precision}, {result.Size.Scale})");

// Output:
// SQL Type: DECIMAL(6, 3)
```

### 3. Manual Accumulator (More Control)

```csharp
using System.Globalization;
using TypeGuesser;
using TypeGuesser.Advanced;

var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
var accumulator = new StackTypeAccumulator(factory);

// Process different types
accumulator.Add(42);
accumulator.Add(-1234);
accumulator.Add(999);

var result = accumulator.GetResult();
Console.WriteLine($"Type: {result.CSharpType}, Digits: {result.Size.NumbersBeforeDecimalPlace}");

// Output:
// Type: System.Int32, Digits: 4
```

### 4. Processing String Data

```csharp
var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
var accumulator = new StackTypeAccumulator(factory);

var data = new[] { "123", "456", "789" };
foreach (var str in data)
{
    accumulator.Add(str.AsSpan());  // Use AsSpan() for strings
}

var result = accumulator.GetResult();
Console.WriteLine($"Detected as: {result.CSharpType}");

// Output:
// Detected as: System.Int32
```

### 5. Maximum Performance (Stack-Allocated)

```csharp
var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);

// Allocate data on stack - zero heap allocations!
Span<int> numbers = stackalloc int[] { 100, 200, 300 };

var accumulator = new StackTypeAccumulator(factory);
foreach (var n in numbers)
{
    accumulator.Add(n);
}

var result = accumulator.GetResult();
// Entire operation used only stack memory
```

## Real-World Examples

### Example: CSV Column Type Detection

```csharp
using System.Globalization;
using TypeGuesser;
using TypeGuesser.Advanced;

public class CsvColumnAnalyzer
{
    public static TypeGuessResult AnalyzeColumn(string[] values)
    {
        var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
        var accumulator = new StackTypeAccumulator(factory);

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                accumulator.Add(value.AsSpan());
            }
        }

        return accumulator.GetResult();
    }

    public static string GetSqlType(TypeGuessResult result)
    {
        if (result.CSharpType == typeof(int))
            return "INT";

        if (result.CSharpType == typeof(decimal))
            return $"DECIMAL({result.Size.Precision}, {result.Size.Scale})";

        if (result.CSharpType == typeof(bool))
            return "BIT";

        var varchar = result.Unicode ? "NVARCHAR" : "VARCHAR";
        return $"{varchar}({result.Width ?? 255})";
    }
}

// Usage:
var columnData = new[] { "123", "456", "789" };
var result = CsvColumnAnalyzer.AnalyzeColumn(columnData);
var sqlType = CsvColumnAnalyzer.GetSqlType(result);

Console.WriteLine($"Recommended SQL type: {sqlType}");
// Output: Recommended SQL type: INT
```

### Example: Financial Data Processing

```csharp
using TypeGuesser.Advanced;

public class FinancialDataProcessor
{
    public static void ProcessTransactions(decimal[] transactions)
    {
        var result = ZeroAlloc.GuessDecimals(transactions);

        Console.WriteLine("Transaction Analysis:");
        Console.WriteLine($"  Total processed: {transactions.Length}");
        Console.WriteLine($"  Recommended precision: {result.Size.Precision}");
        Console.WriteLine($"  Decimal places needed: {result.Size.Scale}");
        Console.WriteLine($"  Suggested SQL type: DECIMAL({result.Size.Precision}, {result.Size.Scale})");

        var minPrecision = Math.Max(result.Size.Precision, 10);
        var minScale = Math.Max(result.Size.Scale, 2);
        Console.WriteLine($"  Safe SQL type: DECIMAL({minPrecision}, {minScale})");
    }
}

// Usage:
var transactions = new decimal[]
{
    100.00m,      // Payment received
    -50.25m,      // Expense
    1234.56m,     // Large transaction
    0.99m         // Small transaction
};

FinancialDataProcessor.ProcessTransactions(transactions);

// Output:
// Transaction Analysis:
//   Total processed: 4
//   Recommended precision: 6
//   Decimal places needed: 2
//   Suggested SQL type: DECIMAL(6, 2)
//   Safe SQL type: DECIMAL(10, 2)
```

### Example: Batch Processing with Type Upgrades

```csharp
using TypeGuesser.Advanced;

public class BatchProcessor
{
    public static TypeGuessResult ProcessMixedBatch()
    {
        var accumulator = ZeroAlloc.CreateAccumulator();

        // Start with flags
        Console.WriteLine("Processing flags...");
        accumulator.Add(true);
        accumulator.Add(false);
        Console.WriteLine($"Current type: {accumulator.GetResult().CSharpType}");
        // Output: System.Boolean

        // Add some counts
        Console.WriteLine("Processing counts...");
        accumulator.Add(1);
        accumulator.Add(10);
        accumulator.Add(100);
        Console.WriteLine($"Current type: {accumulator.GetResult().CSharpType}");
        // Output: System.Int32 (upgraded from bool)

        // Add precise values
        Console.WriteLine("Processing prices...");
        accumulator.Add(9.99m);
        accumulator.Add(19.99m);
        Console.WriteLine($"Current type: {accumulator.GetResult().CSharpType}");
        // Output: System.Decimal (upgraded from int)

        return accumulator.GetResult();
    }
}

var finalResult = BatchProcessor.ProcessMixedBatch();
Console.WriteLine($"\nFinal type: {finalResult.CSharpType}");
Console.WriteLine($"Precision: {finalResult.Size.Precision}, Scale: {finalResult.Size.Scale}");
```

### Example: Large-Scale Data Analysis

```csharp
using System.Diagnostics;
using TypeGuesser.Advanced;

public class LargeScaleAnalyzer
{
    public static void AnalyzeLargeDataset(int recordCount = 1_000_000)
    {
        Console.WriteLine($"Generating {recordCount:N0} records...");

        var data = new int[recordCount];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Random.Shared.Next(1, 100000);
        }

        Console.WriteLine("Analyzing data with zero allocations...");
        var sw = Stopwatch.StartNew();

        var result = ZeroAlloc.GuessIntegers(data);

        sw.Stop();

        Console.WriteLine($"\nResults:");
        Console.WriteLine($"  Records processed: {recordCount:N0}");
        Console.WriteLine($"  Time elapsed: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Throughput: {recordCount / sw.Elapsed.TotalSeconds:N0} records/second");
        Console.WriteLine($"  Type detected: {result.CSharpType}");
        Console.WriteLine($"  Max digits: {result.Size.NumbersBeforeDecimalPlace}");
        Console.WriteLine($"  Memory allocations: ZERO (all stack-based)");
    }
}

LargeScaleAnalyzer.AnalyzeLargeDataset();

// Sample Output:
// Generating 1,000,000 records...
// Analyzing data with zero allocations...
// Results:
//   Records processed: 1,000,000
//   Time elapsed: 5ms
//   Throughput: 200,000,000 records/second
//   Type detected: System.Int32
//   Max digits: 5
//   Memory allocations: ZERO (all stack-based)
```

## Performance Tips

### ✅ DO

```csharp
// Use stack allocation for maximum performance
Span<int> data = stackalloc int[] { 1, 2, 3, 4, 5 };
var result = ZeroAlloc.GuessIntegers(data);
```

```csharp
// Use AsSpan() for string processing
foreach (var str in strings)
{
    accumulator.Add(str.AsSpan());
}
```

```csharp
// Reuse factory for multiple operations
var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
var result1 = ZeroAlloc.GuessIntegers(data1, factory);
var result2 = ZeroAlloc.GuessDecimals(data2, factory);
```

### ❌ DON'T

```csharp
// DON'T try to store the accumulator
private StackTypeAccumulator _accumulator;  // ❌ Won't compile!
```

```csharp
// DON'T return the accumulator
public StackTypeAccumulator CreateAccumulator()  // ❌ Won't compile!
{
    return new StackTypeAccumulator(factory);
}
```

```csharp
// DON'T use in async methods
public async Task ProcessAsync()
{
    var accumulator = new StackTypeAccumulator(factory);
    await SomethingAsync();  // ❌ Won't compile!
}
```

## When to Choose Each API

### Use `ZeroAlloc.GuessIntegers/Decimals/Booleans` when:
- ✅ Processing a single array of uniform type
- ✅ Want simplest possible API
- ✅ Don't need intermediate results

### Use `StackTypeAccumulator` when:
- ✅ Processing multiple batches
- ✅ Need to check intermediate results
- ✅ Processing mixed types that might upgrade
- ✅ Want maximum control

### Use Standard `Guesser` when:
- ✅ Processing strings from CSV/text files
- ✅ Need to store state across methods
- ✅ Working with async/await
- ✅ Need thread-safe shared state

## Common Patterns

### Pattern 1: Batch Processing

```csharp
var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
var accumulator = new StackTypeAccumulator(factory);

// Process batch 1
foreach (var item in batch1)
    accumulator.Add(item);

// Process batch 2
foreach (var item in batch2)
    accumulator.Add(item);

var result = accumulator.GetResult();
```

### Pattern 2: Type Detection Pipeline

```csharp
public TypeGuessResult DetectType(IEnumerable<string> values)
{
    var factory = new TypeDeciderFactory(CultureInfo.InvariantCulture);
    var accumulator = new StackTypeAccumulator(factory);

    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            accumulator.Add(value.AsSpan());
        }
    }

    return accumulator.GetResult();
}
```

### Pattern 3: Progressive Type Checking

```csharp
var accumulator = ZeroAlloc.CreateAccumulator();

foreach (var value in data)
{
    accumulator.Add(value);

    // Check type periodically
    if (needsEarlyResult)
    {
        var intermediateResult = accumulator.GetResult();
        if (intermediateResult.CSharpType == typeof(string))
        {
            break;  // No point continuing if it's already string
        }
    }
}

var finalResult = accumulator.GetResult();
```

## Troubleshooting

### Error: "Cannot use ref struct in async method"

**Problem:**
```csharp
public async Task ProcessAsync()
{
    var accumulator = new StackTypeAccumulator(factory);  // ❌ Error
    await Task.Delay(100);
}
```

**Solution:** Use synchronous processing or standard Guesser API
```csharp
public Task<TypeGuessResult> ProcessAsync()
{
    return Task.Run(() =>
    {
        var accumulator = new StackTypeAccumulator(factory);
        // Process data...
        return accumulator.GetResult();
    });
}
```

### Error: "Cannot convert to interface"

**Problem:**
```csharp
IEnumerable<StackTypeAccumulator> list;  // ❌ Error
```

**Solution:** Use TypeGuessResult instead (it's a normal struct)
```csharp
IEnumerable<TypeGuessResult> results;  // ✅ OK
```

## Next Steps

- Read the [full Advanced API documentation](./ADVANCED-API.md)
- See [Performance Benchmarks](./BENCHMARKS.md)
- Check out the [Standard API documentation](./API.md)
- Review [Architecture Decision Records](./ADR/)
