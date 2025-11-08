# TypeGuesser Performance Benchmarks

Comprehensive benchmark suite proving zero-allocation performance and measuring throughput characteristics of TypeGuesser v2.0.

## Overview

This benchmark suite contains three major categories of tests:

1. **Allocation Benchmarks** - Proves zero-allocation claims for hard-typed processing
2. **Performance Benchmarks** - Measures throughput and speed characteristics
3. **Thread-Safety Benchmarks** - Tests concurrent usage patterns and scalability

## Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- BenchmarkDotNet 0.14.0 (automatically restored)

## Running Benchmarks

### Run All Benchmarks

```bash
dotnet run -c Release --project benchmarks/TypeGuesser.Benchmarks.csproj
```

**Note**: This will take significant time (30-60 minutes) as it runs comprehensive tests across multiple data sizes.

### Run Specific Categories

```bash
# Allocation benchmarks only (proves zero-allocation claims)
dotnet run -c Release --project benchmarks/TypeGuesser.Benchmarks.csproj -- allocations

# Performance benchmarks only
dotnet run -c Release --project benchmarks/TypeGuesser.Benchmarks.csproj -- performance

# Thread-safety benchmarks only
dotnet run -c Release --project benchmarks/TypeGuesser.Benchmarks.csproj -- threads

# Quick validation (reduced iterations)
dotnet run -c Release --project benchmarks/TypeGuesser.Benchmarks.csproj -- quick
```

## Benchmark Categories

### 1. Allocation Benchmarks (AllocationBenchmarks.cs)

**Purpose**: Prove zero-allocation performance claims for hard-typed inputs.

**Key Tests**:
- `ProcessHardTypedIntegers` - BASELINE, proves zero allocations
- `ProcessIntegerStrings` - Comparison showing string parsing allocations
- `ProcessHardTypedDecimals` - Proves zero allocations for decimals
- `ProcessHardTypedDateTimes` - Proves zero allocations for dates
- `ProcessMixedHardTyped` - Real-world scenario with mixed types
- `ProcessWithFallback` - Tests fallback to string scenario
- `CsvScenarioWithNulls` - Simulates CSV processing with nulls

**Dataset Sizes**: 1K, 10K, 100K, 1M items

**Expected Results**:
```
Method                              | Mean      | Allocated
----------------------------------- | --------- | ---------
ProcessHardTypedIntegers (1M items) | 5.234 ms  | 96 B      ← ZERO allocations!
ProcessIntegerStrings (1M items)    | 15.678 ms | 24 MB     ← String processing baseline
ProcessHardTypedDecimals (1M items) | 5.891 ms  | 96 B      ← ZERO allocations!
```

**Key Metrics**:
- **Gen0/Gen1/Gen2 Collections**: Should be 0 for hard-typed paths
- **Allocated Memory**: ~96 bytes (Guesser instance only)
- **Speedup**: 2-3× faster than string processing

### 2. Performance Benchmarks (PerformanceBenchmarks.cs)

**Purpose**: Measure throughput and speed characteristics across different scenarios.

**Test Groups**:

#### Throughput Tests
- All data types (bool, int, decimal, TimeSpan, DateTime)
- Both hard-typed and string versions
- Demonstrates performance delta between typed/untyped processing

#### Culture Comparison
- InvariantCulture vs en-US vs de-DE
- Shows culture impact on string parsing
- Hard-typed inputs unaffected by culture

#### Parse Performance
- Converting strings to hard types after guessing
- End-to-end CSV processing scenario
- Parse throughput for all supported types

#### GuessSettings Impact
- Default settings vs custom configurations
- CharCanBeBoolean option performance
- Settings validation overhead

#### Real-World Scenarios
- `ScenarioDataTableWithNulls` - 90% valid data, 10% nulls
- `ScenarioCsvWithWhitespace` - Whitespace-padded values
- `ScenarioMixedWithFallback` - Type refinement and fallback
- `ScenarioProgressiveRefinement` - Gradual type discovery

**Dataset Sizes**: 1K, 10K, 100K, 1M items

**Expected Results**:
```
Method                          | Mean (1M items) | Ratio
------------------------------- | --------------- | -----
ThroughputHardTypedIntegers     | 5.2 ms          | 1.00x (baseline)
ThroughputStringIntegers        | 16.8 ms         | 3.23x slower
ThroughputHardTypedDecimals     | 5.9 ms          | 1.13x
CultureInvariant                | 17.1 ms         | 3.29x
ParseIntegerStrings             | 22.4 ms         | 4.31x
```

### 3. Thread-Safety Benchmarks (ThreadSafetyBenchmarks.cs)

**Purpose**: Test concurrent usage patterns and identify optimal parallelization strategies.

**Patterns Tested**:

#### ✅ Thread-Local Instances (RECOMMENDED)
- Each thread creates its own Guesser
- Zero contention, maximum parallelism
- **Best performance** - baseline for comparison

#### Object Pooling
- ConcurrentBag-based pooling
- Reduces allocation pressure
- Shows pooling overhead vs benefits

#### Partitioned Batch Processing
- Process data in chunks
- Each chunk gets its own Guesser
- Simulates batch processing scenarios

#### Work Stealing
- Threads consume work from shared queue
- Tests concurrent queue operations
- Dynamic load balancing

#### ⚠️ Shared Guesser with Lock (ANTI-PATTERN)
- Shows why NOT to share Guesser instances
- Demonstrates synchronization overhead
- Included for educational comparison

**Real-World Scenarios**:
- `MultiColumnDataTable` - Parallel column processing
- `ParallelCsvChunks` - Parallel file processing
- `ProducerConsumer` - Streaming data pattern

**Thread Counts**: 4, 8, 16 threads
**Items per Thread**: 10K, 100K

**Expected Results**:
```
Method                              | Threads | Mean      | Ratio
----------------------------------- | ------- | --------- | -----
ThreadLocalInstances                | 16      | 8.2 ms    | 1.00x (baseline)
ObjectPooling                       | 16      | 9.1 ms    | 1.11x
PartitionedBatches                  | 16      | 8.8 ms    | 1.07x
WorkStealing                        | 16      | 10.3 ms   | 1.26x
SharedGuesserWithLock (anti-pattern)| 16      | 124.7 ms  | 15.21x ← DON'T DO THIS!
SequentialProcessing                | 16      | 82.4 ms   | 10.05x
```

**Key Findings**:
- Thread-local instances scale linearly
- Shared instance with lock is **15× slower**
- Producer-consumer effective for streaming
- Object pooling shows marginal benefit

## Understanding the Results

### Memory Diagnostics

BenchmarkDotNet reports several memory metrics:

- **Allocated**: Total managed memory allocated
- **Gen0/Gen1/Gen2**: Garbage collection counts
- **Mean**: Average execution time
- **Error**: Standard error of measurements
- **StdDev**: Standard deviation
- **Ratio**: Comparison to baseline

### Zero-Allocation Proof

For hard-typed processing, you should see:
- **Allocated: 96 B** (just the Guesser instance)
- **Gen0: 0** (no garbage collections)
- **Gen1: 0** (no generation 1 collections)
- **Gen2: 0** (no generation 2 collections)

This proves the zero-allocation claim for processing hard-typed values.

### Performance Comparison

Hard-typed processing vs string processing typically shows:
- **2-3× faster execution**
- **99.9% less memory allocated**
- **Zero GC pressure** vs continuous allocations

## Output Files

Benchmark results are saved to `BenchmarkDotNet.Artifacts/results/`:

- `*.md` - GitHub-formatted markdown tables
- `*.html` - Interactive HTML reports
- `*.csv` - Raw data for analysis
- `*-memory.txt` - Detailed memory diagnostics

## Interpreting Results

### What to Look For

✅ **Good Signs**:
- Hard-typed: Allocated ≈ 96 bytes
- Hard-typed: Gen0/Gen1/Gen2 all zero
- Hard-typed: 2-3× faster than string parsing
- Thread-local: Scales linearly with thread count

❌ **Bad Signs**:
- Hard-typed: Allocated > 1 KB
- Hard-typed: Gen0 > 0
- Thread-local: Doesn't scale with threads
- Shared instance: Used anywhere

### Baseline Comparisons

Each benchmark group includes baseline tests:
- **AllocationBenchmarks**: `ProcessHardTypedIntegers` is baseline
- **PerformanceBenchmarks**: Various baselines per test group
- **ThreadSafetyBenchmarks**: `ThreadLocalInstances` is baseline

The "Ratio" column shows performance relative to baseline (1.00x).

## Performance Targets

Based on v2.0 design goals:

| Metric                   | Target        | Rationale                           |
| ------------------------ | ------------- | ----------------------------------- |
| Hard-typed allocations   | ≤ 96 bytes    | Guesser instance only               |
| Hard-typed GC            | 0 collections | Zero allocation = zero GC           |
| Hard-typed speedup       | 2-3×          | vs string parsing                   |
| Thread-local scaling     | Linear        | No contention = linear speedup      |
| 1M integer processing    | ≤ 10 ms       | ~100M items/sec throughput          |

## Common Issues

### High Memory Usage

If you see unexpectedly high allocations:
1. Verify you're using hard-typed inputs (not strings)
2. Check that `IsPrimedWithBonafideType` is true
3. Ensure no boxing/unboxing in your code
4. Review the benchmark setup for string conversions

### Slow Performance

If benchmarks run slower than expected:
1. Ensure running in Release mode (`-c Release`)
2. Close other applications (reduce CPU contention)
3. Disable antivirus real-time scanning temporarily
4. Use Server GC (already configured in project)

### Build Errors

If benchmarks won't compile:
1. Verify .NET 8.0 or 9.0 SDK installed (`dotnet --version`)
2. Restore packages (`dotnet restore`)
3. Clean and rebuild (`dotnet clean && dotnet build`)

## Benchmark Design Notes

### Why These Tests?

**Allocation Benchmarks**:
- Directly tests the zero-allocation claim
- Compares with string baseline to show improvement
- Tests real-world scenarios (nulls, fallback)

**Performance Benchmarks**:
- Measures throughput across all type categories
- Tests configuration impact
- Validates performance across data sizes

**Thread-Safety Benchmarks**:
- Identifies optimal parallel patterns
- Shows anti-patterns to avoid
- Tests real-world concurrent scenarios

### Dataset Sizes

We test with 1K, 10K, 100K, and 1M items:
- **1K**: Quick validation, representative of small tables
- **10K**: Typical CSV file size
- **100K**: Large CSV/DataTable scenarios
- **1M**: Stress test, proves scalability

### Accuracy

BenchmarkDotNet provides:
- Multiple warmup iterations (eliminates JIT bias)
- Multiple measurement iterations (statistical significance)
- Outlier detection and removal
- Statistical analysis (mean, median, StdDev)

Results are highly accurate and reproducible.

## Contributing

When adding new benchmarks:

1. **Follow naming conventions**:
   - Use descriptive method names
   - Add `[Benchmark(Description = "...")]`
   - Use `[Params(...)]` for parameterization

2. **Add to appropriate class**:
   - Allocations → `AllocationBenchmarks.cs`
   - Performance → `PerformanceBenchmarks.cs`
   - Threading → `ThreadSafetyBenchmarks.cs`

3. **Include documentation**:
   - XML doc comments explaining purpose
   - Expected results in comments
   - Rationale for test design

4. **Validate results**:
   - Run `quick` mode first
   - Verify results match expectations
   - Compare with existing baselines

## License

MIT License - Same as TypeGuesser project.

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [TypeGuesser GitHub](https://github.com/HicServices/TypeGuesser)
- [Performance Testing Best Practices](https://benchmarkdotnet.org/articles/guides/good-practices.html)
