# Standard API Reference

Complete API documentation for the `Guesser` class - the primary interface for type guessing in TypeGuesser v2.0.

## Table of Contents

1. [Overview](#overview)
2. [Class: Guesser](#class-guesser)
3. [Constructors](#constructors)
4. [Properties](#properties)
5. [Methods](#methods)
6. [Supporting Types](#supporting-types)
7. [Usage Examples](#usage-examples)
8. [Thread Safety](#thread-safety)
9. [Performance Tips](#performance-tips)
10. [Common Patterns](#common-patterns)

---

## Overview

The `Guesser` class analyzes values to determine the most appropriate database type for storing them. It starts with the most restrictive type (e.g., bool, int) and progressively falls back to more general types (e.g., decimal, string) as needed.

### Key Features

- **Automatic Type Detection**: Analyzes values and selects optimal C# and database types
- **Size Calculation**: Determines precision, scale, and width requirements
- **Thread Safety**: Safe for concurrent access (v2.0+)
- **Zero-Allocation Optimizations**: Fast processing for hard-typed values (v2.0+)
- **Culture Support**: Respects culture-specific formatting rules
- **DataTable Integration**: Works seamlessly with ADO.NET DataTables
- **Value Parsing**: Converts strings back to hard-typed values

### Type Preference Order

TypeGuesser tries types in this order (most restrictive first):

1. `bool` - Boolean values
2. `int` - Integer values
3. `decimal` - Decimal/numeric values
4. `TimeSpan` - Time durations
5. `DateTime` - Date/time values
6. `string` - Text values (fallback)

Once a more general type is selected, the guesser never reverts to a more restrictive type.

---

## Class: Guesser

```csharp
namespace TypeGuesser;

public sealed class Guesser : IDisposable
```

### Namespace

`TypeGuesser`

### Inheritance

- `object`
  - `Guesser`

### Implements

- `IDisposable`

---

## Constructors

### Guesser()

Creates a new `Guesser` instance with default settings.

```csharp
public Guesser()
```

**Example:**

```csharp
using TypeGuesser;

var guesser = new Guesser();
guesser.AdjustToCompensateForValue("42");
guesser.AdjustToCompensateForValue("99");

var guess = guesser.Guess;
// guess.CSharpType => typeof(int)
```

### Guesser(DatabaseTypeRequest)

Creates a new `Guesser` primed with an initial type hint.

```csharp
public Guesser(DatabaseTypeRequest request)
```

**Parameters:**

- `request` (`DatabaseTypeRequest`) - Initial type configuration

**Example:**

```csharp
// Start with decimal type hint
var request = new DatabaseTypeRequest(typeof(decimal))
{
    Size = new DecimalSize { Precision = 10, Scale = 2 }
};

var guesser = new Guesser(request);
guesser.AdjustToCompensateForValue(123.45m);

var guess = guesser.Guess;
// guess.CSharpType => typeof(decimal)
```

**Throws:**

- `NotSupportedException` - If the requested type is not supported

---

## Properties

### Guess

Gets the currently computed database type that can store all values processed so far.

```csharp
public DatabaseTypeRequest Guess { get; }
```

**Returns:** `DatabaseTypeRequest` containing:
- `CSharpType` - The C# type (e.g., `typeof(int)`)
- `Size` - Precision/scale information for decimals
- `Width` - Maximum character width for strings
- Other database-specific metadata

**Example:**

```csharp
var guesser = new Guesser();
guesser.AdjustToCompensateForValue("12.45");
guesser.AdjustToCompensateForValue("999.99");

var guess = guesser.Guess;

Console.WriteLine($"Type: {guess.CSharpType.Name}");
Console.WriteLine($"Width: {guess.Width}");
Console.WriteLine($"Precision: {guess.Size.Precision}");
Console.WriteLine($"Scale: {guess.Size.Scale}");

// Output:
// Type: Decimal
// Width: 6
// Precision: 5
// Scale: 2
```

### Settings

Gets the settings that control type guessing behavior.

```csharp
public GuessSettings Settings { get; }
```

**Returns:** `GuessSettings` configuration object

**Example:**

```csharp
var guesser = new Guesser();

// Configure behavior
guesser.Settings.CharCanBeBoolean = true;
guesser.Settings.AllowInterpretingNumbers = true;

guesser.AdjustToCompensateForValue("Y");
var guess = guesser.Guess;
// guess.CSharpType => typeof(bool) (because 'Y' is interpreted as boolean)
```

### Culture

Sets the culture for parsing values (decimal separators, date formats, etc.).

```csharp
public CultureInfo Culture { set; }
```

**Example:**

```csharp
var guesser = new Guesser
{
    Culture = new CultureInfo("fr-FR") // French culture
};

guesser.AdjustToCompensateForValue("12,45"); // Comma as decimal separator
guesser.AdjustToCompensateForValue("99,99");

var guess = guesser.Guess;
// guess.CSharpType => typeof(decimal)
```

### IsPrimedWithBonafideType

Gets whether the guesser has been primed with hard-typed values (not strings).

```csharp
public bool IsPrimedWithBonafideType { get; }
```

**Returns:** `true` if hard-typed values have been processed; otherwise `false`

**Example:**

```csharp
var guesser = new Guesser();

Console.WriteLine(guesser.IsPrimedWithBonafideType); // false

guesser.AdjustToCompensateForValue(42); // Hard-typed int

Console.WriteLine(guesser.IsPrimedWithBonafideType); // true
```

**Important:** Once primed with hard-typed values, mixing string values will throw `MixedTypingException`.

### ExtraLengthPerNonAsciiCharacter

Gets or sets extra width to allocate for non-ASCII characters (useful for databases like Oracle).

```csharp
public int ExtraLengthPerNonAsciiCharacter { get; init; }
```

**Default:** `0`

**Example:**

```csharp
var guesser = new Guesser
{
    ExtraLengthPerNonAsciiCharacter = 2 // Oracle NVARCHAR overhead
};

guesser.AdjustToCompensateForValue("café"); // Contains 'é' (non-ASCII)

var guess = guesser.Guess;
// Width accounts for extra bytes needed for non-ASCII characters
```

---

## Methods

### AdjustToCompensateForValue(object?)

Processes a single value and updates the type guess accordingly.

```csharp
public void AdjustToCompensateForValue(object? o)
```

**Parameters:**

- `o` (`object?`) - The value to process. Can be:
  - `string` - Parsed to determine type
  - `int`, `decimal`, `bool`, etc. - Hard-typed values (zero-allocation in v2.0)
  - `null` or `DBNull.Value` - Ignored (doesn't affect guess)

**Throws:**

- `MixedTypingException` - If mixing hard-typed values with strings
- `ObjectDisposedException` - If called after `Dispose()`

**Example:**

```csharp
var guesser = new Guesser();

// String values (parsed)
guesser.AdjustToCompensateForValue("42");
guesser.AdjustToCompensateForValue("99");
guesser.AdjustToCompensateForValue("12.5"); // Upgrades to decimal

var guess = guesser.Guess;
// guess.CSharpType => typeof(decimal)
```

**Performance Tip (v2.0):** Pass hard-typed values for zero-allocation processing:

```csharp
var guesser = new Guesser();

// Hard-typed values (zero allocations!)
guesser.AdjustToCompensateForValue(42);
guesser.AdjustToCompensateForValue(99);
guesser.AdjustToCompensateForValue(12.5m);

var guess = guesser.Guess;
// Much faster than string conversion
```

### AdjustToCompensateForValues(IEnumerable&lt;object&gt;)

Processes multiple values from a collection.

```csharp
public void AdjustToCompensateForValues(IEnumerable<object> collection)
```

**Parameters:**

- `collection` (`IEnumerable<object>`) - Values to process

**Throws:**

- `ObjectDisposedException` - If called after `Dispose()`

**Example:**

```csharp
var values = new object[] { "1", "42", "999", "-5" };
var guesser = new Guesser();

guesser.AdjustToCompensateForValues(values);

var guess = guesser.Guess;
// guess.CSharpType => typeof(int)
// guess.Size.NumbersBeforeDecimalPlace => 3
```

### AdjustToCompensateForValues(DataColumn)

Processes all values in a DataTable column.

```csharp
public void AdjustToCompensateForValues(DataColumn column)
```

**Parameters:**

- `column` (`DataColumn`) - The column to analyze

**Throws:**

- `ObjectDisposedException` - If called after `Dispose()`

**Example:**

```csharp
var table = new DataTable();
table.Columns.Add("Amount", typeof(object));
table.Rows.Add("10.50");
table.Rows.Add("25.75");
table.Rows.Add("100.00");

var guesser = new Guesser();
guesser.AdjustToCompensateForValues(table.Columns["Amount"]);

var guess = guesser.Guess;
// guess.CSharpType => typeof(decimal)
```

### ShouldDowngradeColumnTypeToMatchCurrentEstimate(DataColumn)

Determines whether the current guess is an improvement over a DataColumn's type.

```csharp
public bool ShouldDowngradeColumnTypeToMatchCurrentEstimate(DataColumn col)
```

**Parameters:**

- `col` (`DataColumn`) - The column to evaluate

**Returns:** `true` if the guessed type is more specific than the column's current type; otherwise `false`

**Throws:**

- `ObjectDisposedException` - If called after `Dispose()`

**Example:**

```csharp
var table = new DataTable();
table.Columns.Add("Amount", typeof(object)); // Generic object column

var guesser = new Guesser();
guesser.AdjustToCompensateForValues(table.Columns["Amount"]);

if (guesser.ShouldDowngradeColumnTypeToMatchCurrentEstimate(table.Columns["Amount"]))
{
    Console.WriteLine($"Recommend changing to: {guesser.Guess.CSharpType.Name}");
}
```

**Note:** Changing a DataColumn's type requires cloning the DataTable. See: [Stack Overflow](https://stackoverflow.com/questions/9028029/how-to-change-datatype-of-a-datacolumn-in-a-datatable)

### Parse(string)

Parses a string value according to the current guess type.

```csharp
public object? Parse(string val)
```

**Parameters:**

- `val` (`string`) - The string to parse

**Returns:** Parsed value with the appropriate type (or `null`)

**Throws:**

- `NotSupportedException` - If the current guess type doesn't have a parser
- `ObjectDisposedException` - If called after `Dispose()`

**Example:**

```csharp
var guesser = new Guesser();
var values = new[] { "13:11:59", "9AM", "14:30:00" };

guesser.AdjustToCompensateForValues(values);

// Parse all values to their hard types
var parsed = values.Select(guesser.Parse).ToArray();

Console.WriteLine($"Type: {guesser.Guess.CSharpType.Name}");
Console.WriteLine($"Value 1: {parsed[0]}"); // TimeSpan: 13:11:59
Console.WriteLine($"Value 2: {parsed[1]}"); // TimeSpan: 09:00:00
Console.WriteLine($"Value 3: {parsed[2]}"); // TimeSpan: 14:30:00
```

### Dispose()

Releases resources and returns the internal pooled builder for reuse.

```csharp
public void Dispose()
```

**Remarks:**

- After calling `Dispose()`, do not call any methods on this instance
- Safe to call multiple times; subsequent calls are no-ops
- Recommended for high-volume scenarios to reduce GC pressure

**Example:**

```csharp
using (var guesser = new Guesser())
{
    guesser.AdjustToCompensateForValue("42");
    var guess = guesser.Guess;
    // Process guess...
} // Dispose() called automatically, builder returned to pool
```

### GetSharedFactory(CultureInfo?)

Gets a shared, thread-safe `TypeDeciderFactory` for advanced scenarios.

```csharp
public static TypeDeciderFactory GetSharedFactory(CultureInfo? culture = null)
```

**Parameters:**

- `culture` (`CultureInfo?`) - The culture to use (defaults to `CurrentCulture`)

**Returns:** Cached, thread-safe `TypeDeciderFactory`

**Example:**

```csharp
// Get factory for invariant culture
var factory = Guesser.GetSharedFactory(CultureInfo.InvariantCulture);

// Use factory for advanced scenarios
var decider = factory.Dictionary[typeof(int)];
var parsed = decider.Parse("42");
```

---

## Supporting Types

### DatabaseTypeRequest

Represents a guessed database type with size information.

```csharp
public class DatabaseTypeRequest
{
    public Type CSharpType { get; set; }
    public DecimalSize Size { get; set; }
    public int? Width { get; set; }
    public bool Unicode { get; set; }

    public static List<Type> PreferenceOrder { get; }
}
```

**Properties:**

- `CSharpType` - The C# type (e.g., `typeof(int)`, `typeof(decimal)`)
- `Size` - Precision and scale for numeric types
- `Width` - Maximum character width for string types
- `Unicode` - Whether non-ASCII characters were detected
- `PreferenceOrder` - Static list defining type preference order

### DecimalSize

Represents numeric precision and scale information.

```csharp
public struct DecimalSize
{
    public byte NumbersBeforeDecimalPlace { get; set; }
    public byte NumbersAfterDecimalPlace { get; set; }
    public byte Precision { get; }
    public byte Scale { get; }
}
```

**Properties:**

- `NumbersBeforeDecimalPlace` - Digits before decimal point
- `NumbersAfterDecimalPlace` - Digits after decimal point
- `Precision` - Total number of significant digits
- `Scale` - Number of decimal places

**Example:**

```csharp
var guesser = new Guesser();
guesser.AdjustToCompensateForValue("123.45");

var size = guesser.Guess.Size;
Console.WriteLine($"Before: {size.NumbersBeforeDecimalPlace}"); // 3
Console.WriteLine($"After: {size.NumbersAfterDecimalPlace}");   // 2
Console.WriteLine($"Precision: {size.Precision}");               // 5
Console.WriteLine($"Scale: {size.Scale}");                       // 2
```

### GuessSettings

Configuration options for type guessing behavior.

```csharp
public class GuessSettings
{
    public bool CharCanBeBoolean { get; set; }
    public bool AllowInterpretingNumbers { get; set; }
    // ... other settings
}
```

**Key Properties:**

- `CharCanBeBoolean` - Allow single characters like 'Y'/'N' to be boolean
- `AllowInterpretingNumbers` - Allow parsing of formatted numbers (e.g., "1,000")

### Exceptions

#### MixedTypingException

Thrown when mixing hard-typed values with string values.

```csharp
var guesser = new Guesser();
guesser.AdjustToCompensateForValue(42); // Hard-typed int

// This throws MixedTypingException:
guesser.AdjustToCompensateForValue("99"); // String
```

**Solution:** Use consistent input types (all strings OR all hard-typed).

---

## Usage Examples

### Example 1: Basic Type Detection

```csharp
using TypeGuesser;

var guesser = new Guesser();

guesser.AdjustToCompensateForValue("42");
guesser.AdjustToCompensateForValue("99");
guesser.AdjustToCompensateForValue("12.5"); // Upgrades to decimal

var guess = guesser.Guess;

Console.WriteLine($"Type: {guess.CSharpType.Name}");          // Decimal
Console.WriteLine($"Precision: {guess.Size.Precision}");      // 3
Console.WriteLine($"Scale: {guess.Size.Scale}");              // 1
Console.WriteLine($"Width: {guess.Width}");                   // 4
```

### Example 2: Processing CSV Data

```csharp
var csvLines = new[]
{
    "Name,Age,Salary",
    "Alice,30,75000.50",
    "Bob,25,62000.75",
    "Carol,35,95000.00"
};

var columns = csvLines[0].Split(',');
var guessers = columns.Select(_ => new Guesser()).ToArray();

// Process each row (skip header)
foreach (var line in csvLines.Skip(1))
{
    var values = line.Split(',');
    for (int i = 0; i < values.Length; i++)
    {
        guessers[i].AdjustToCompensateForValue(values[i]);
    }
}

// Report results
for (int i = 0; i < columns.Length; i++)
{
    var guess = guessers[i].Guess;
    Console.WriteLine($"{columns[i]}: {guess.CSharpType.Name} (Width: {guess.Width})");
}

// Output:
// Name: String (Width: 5)
// Age: Int32 (Width: 2)
// Salary: Decimal (Width: 8)
```

### Example 3: DataTable Type Optimization

```csharp
// Load CSV into DataTable with all object columns
var table = new DataTable();
table.Columns.Add("Id", typeof(object));
table.Columns.Add("Amount", typeof(object));
table.Columns.Add("Date", typeof(object));

table.Rows.Add("1", "10.50", "2024-01-01");
table.Rows.Add("2", "25.75", "2024-01-02");
table.Rows.Add("3", "100.00", "2024-01-03");

// Analyze each column
foreach (DataColumn column in table.Columns)
{
    var guesser = new Guesser();
    guesser.AdjustToCompensateForValues(column);

    if (guesser.ShouldDowngradeColumnTypeToMatchCurrentEstimate(column))
    {
        var guess = guesser.Guess;
        Console.WriteLine($"Column '{column.ColumnName}' should be {guess.CSharpType.Name}");
    }
}

// Output:
// Column 'Id' should be Int32
// Column 'Amount' should be Decimal
// Column 'Date' should be DateTime
```

### Example 4: Hard-Typed JSON Data

```csharp
using System.Text.Json;

var json = """
[
    {"id": 1, "price": 10.50, "active": true},
    {"id": 2, "price": 25.75, "active": false},
    {"id": 3, "price": 100.00, "active": true}
]
""";

var items = JsonSerializer.Deserialize<List<JsonElement>>(json);

var idGuesser = new Guesser();
var priceGuesser = new Guesser();
var activeGuesser = new Guesser();

foreach (var item in items)
{
    // Pass hard-typed values for zero-allocation processing
    idGuesser.AdjustToCompensateForValue(item.GetProperty("id").GetInt32());
    priceGuesser.AdjustToCompensateForValue(item.GetProperty("price").GetDecimal());
    activeGuesser.AdjustToCompensateForValue(item.GetProperty("active").GetBoolean());
}

Console.WriteLine($"ID: {idGuesser.Guess.CSharpType.Name}");          // Int32
Console.WriteLine($"Price: {priceGuesser.Guess.CSharpType.Name}");    // Decimal
Console.WriteLine($"Active: {activeGuesser.Guess.CSharpType.Name}");  // Boolean
```

### Example 5: Culture-Specific Parsing

```csharp
// European number format (comma as decimal separator)
var guesserEU = new Guesser
{
    Culture = new CultureInfo("de-DE") // German culture
};

guesserEU.AdjustToCompensateForValue("12,45");
guesserEU.AdjustToCompensateForValue("99,99");

Console.WriteLine(guesserEU.Guess.CSharpType.Name); // Decimal

// US number format (period as decimal separator)
var guesserUS = new Guesser
{
    Culture = new CultureInfo("en-US")
};

guesserUS.AdjustToCompensateForValue("12.45");
guesserUS.AdjustToCompensateForValue("99.99");

Console.WriteLine(guesserUS.Guess.CSharpType.Name); // Decimal
```

### Example 6: Date and Time Detection

```csharp
var guesser = new Guesser();

// Date detection
guesser.AdjustToCompensateForValue("2024-01-01");
guesser.AdjustToCompensateForValue("2024-12-31");

Console.WriteLine(guesser.Guess.CSharpType.Name); // DateTime

// Time detection
var timeGuesser = new Guesser();
timeGuesser.AdjustToCompensateForValue("13:11:59");
timeGuesser.AdjustToCompensateForValue("9AM");
timeGuesser.AdjustToCompensateForValue("14:30:00");

Console.WriteLine(timeGuesser.Guess.CSharpType.Name); // TimeSpan
```

### Example 7: Handling Nulls

```csharp
var guesser = new Guesser();

guesser.AdjustToCompensateForValue("42");
guesser.AdjustToCompensateForValue(null);           // Ignored
guesser.AdjustToCompensateForValue(DBNull.Value);   // Ignored
guesser.AdjustToCompensateForValue("99");

var guess = guesser.Guess;
Console.WriteLine(guess.CSharpType.Name); // Int32

// Null values don't affect the type guess
```

---

## Thread Safety

### v2.0+ Thread-Safe Operations

The `Guesser` class uses internal locking to ensure thread-safe concurrent access:

```csharp
var guesser = new Guesser();
var data = Enumerable.Range(1, 100_000).Select(i => (object)i).ToArray();

// Safe to process concurrently
Parallel.ForEach(data, value =>
{
    guesser.AdjustToCompensateForValue(value); // Thread-safe!
});

var guess = guesser.Guess;
Console.WriteLine($"Type: {guess.CSharpType.Name}"); // Int32
```

### Per-Thread Instances (Alternative)

For maximum performance, consider using separate instances per thread:

```csharp
var results = new ConcurrentBag<DatabaseTypeRequest>();

Parallel.ForEach(dataPartitions, partition =>
{
    var guesser = new Guesser(); // Thread-local instance

    foreach (var value in partition)
    {
        guesser.AdjustToCompensateForValue(value);
    }

    results.Add(guesser.Guess);
});

// Merge results from all threads
var finalGuess = MergeGuesses(results);
```

---

## Performance Tips

### 1. Use Hard-Typed Values (v2.0+)

**❌ Slow - String Conversion:**

```csharp
var guesser = new Guesser();
int[] numbers = { 1, 42, 999 };

foreach (var num in numbers)
{
    guesser.AdjustToCompensateForValue(num.ToString()); // Allocates!
}
// Time: ~850ms for 1M values, Allocations: 76 MB
```

**✅ Fast - Direct Typed Values:**

```csharp
var guesser = new Guesser();
int[] numbers = { 1, 42, 999 };

foreach (var num in numbers)
{
    guesser.AdjustToCompensateForValue(num); // Zero allocations!
}
// Time: ~45ms for 1M values, Allocations: 0 bytes
// 18.9x faster!
```

### 2. Use Dispose for High-Volume Scenarios

```csharp
// Return pooled builder for reuse
using (var guesser = new Guesser())
{
    ProcessLargeDataset(guesser);
} // Dispose() returns builder to pool
```

### 3. Avoid Repeated Guess Access in Loops

**❌ Inefficient:**

```csharp
foreach (var value in largeDataset)
{
    guesser.AdjustToCompensateForValue(value);
    var type = guesser.Guess.CSharpType; // Expensive!
}
```

**✅ Efficient:**

```csharp
foreach (var value in largeDataset)
{
    guesser.AdjustToCompensateForValue(value);
}

var guess = guesser.Guess; // Access once after loop
```

### 4. Batch Process Collections

Use `AdjustToCompensateForValues` for collections instead of looping manually:

```csharp
// Good - single method call
guesser.AdjustToCompensateForValues(largeCollection);

// Also good - equivalent performance
foreach (var value in largeCollection)
{
    guesser.AdjustToCompensateForValue(value);
}
```

---

## Common Patterns

### Pattern 1: Type Detection for Database Schema Generation

```csharp
public string GenerateDDL(DataTable table)
{
    var ddl = new StringBuilder($"CREATE TABLE {table.TableName} (\n");

    foreach (DataColumn column in table.Columns)
    {
        var guesser = new Guesser();
        guesser.AdjustToCompensateForValues(column);

        var guess = guesser.Guess;
        var sqlType = ConvertToSqlType(guess);

        ddl.AppendLine($"  {column.ColumnName} {sqlType},");
    }

    ddl.AppendLine(");");
    return ddl.ToString();
}

private string ConvertToSqlType(DatabaseTypeRequest guess)
{
    return guess.CSharpType.Name switch
    {
        "Int32" => "INT",
        "Decimal" => $"DECIMAL({guess.Size.Precision}, {guess.Size.Scale})",
        "DateTime" => "DATETIME",
        "TimeSpan" => "TIME",
        "Boolean" => "BIT",
        _ => $"VARCHAR({guess.Width})"
    };
}
```

### Pattern 2: Stream Processing with Sampling

```csharp
public DatabaseTypeRequest GuessTypeFromStream(IEnumerable<string> stream, int sampleSize = 1000)
{
    var guesser = new Guesser();

    foreach (var value in stream.Take(sampleSize))
    {
        guesser.AdjustToCompensateForValue(value);
    }

    return guesser.Guess;
}
```

### Pattern 3: Multi-Column Analysis

```csharp
public Dictionary<string, DatabaseTypeRequest> AnalyzeColumns(List<Dictionary<string, object>> rows)
{
    if (!rows.Any()) return new Dictionary<string, DatabaseTypeRequest>();

    // Create guesser for each column
    var columnNames = rows[0].Keys;
    var guessers = columnNames.ToDictionary(name => name, _ => new Guesser());

    // Process all rows
    foreach (var row in rows)
    {
        foreach (var kvp in row)
        {
            guessers[kvp.Key].AdjustToCompensateForValue(kvp.Value);
        }
    }

    // Return results
    return guessers.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.Guess
    );
}
```

---

## See Also

- [Advanced API Documentation](./ADVANCED-API.md) - `StackTypeAccumulator` for maximum performance
- [API Layers Reference](./API-LAYERS.md) - Complete overview of all three API layers
- [Performance Benchmarks](./BENCHMARKS.md) - Detailed performance comparisons
- [Migration Guide](../MIGRATION-V2.md) - Upgrading from v1.x to v2.0
- [Zero-Allocation Guide](./ZERO-ALLOCATION-GUIDE.md) - Deep dive into allocation-free design
- [Thread-Safety Guide](./THREAD-SAFETY.md) - Concurrent usage patterns
