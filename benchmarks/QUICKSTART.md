# TypeGuesser Benchmarks - Quick Start Guide

Get started with benchmarking TypeGuesser performance in 5 minutes.

## Prerequisites

Install .NET 8.0 or .NET 9.0 SDK:
- Download from https://dotnet.microsoft.com/download
- Verify: `dotnet --version`

## Quick Commands

### 1. Validate Setup (30 seconds)

```bash
cd benchmarks
dotnet build -c Release
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Quick Test (5 minutes)

Run a fast validation with reduced iterations:

```bash
dotnet run -c Release -- quick AllocationBenchmarks
```

This tests the core zero-allocation claim with minimal time investment.

### 3. Prove Zero Allocations (15 minutes)

Run full allocation benchmarks to prove zero-allocation performance:

```bash
dotnet run -c Release -- allocations
```

Look for these key metrics:
```
ProcessHardTypedIntegers | 1000000 | 5.2 ms | 96 B      ← Only 96 bytes!
ProcessIntegerStrings    | 1000000 | 16.8 ms| 24 MB     ← Baseline comparison
```

### 4. Full Benchmark Suite (30-60 minutes)

Run all benchmarks including performance and threading tests:

```bash
dotnet run -c Release
```

## Understanding Output

### Example Output

```
| Method                      | N       | Mean    | Allocated |
| --------------------------- | ------- | ------- | --------- |
| ProcessHardTypedIntegers    | 1000000 | 5.23 ms | 96 B      |
```

**What this means**:
- **Method**: Test being run
- **N**: Number of items processed
- **Mean**: Average execution time
- **Allocated**: Total memory allocated

### Zero-Allocation Proof

Look for these values in hard-typed tests:
- ✅ **Allocated: 96 B** (or close to it)
- ✅ **Gen0: 0.0000** (no GC)
- ✅ **Gen1: 0.0000** (no GC)
- ✅ **Gen2: 0.0000** (no GC)

### Performance Comparison

Compare ratios:
```
ProcessHardTypedIntegers    | 1.00x  (baseline)
ProcessIntegerStrings       | 3.23x  (3.2× slower)
```

This shows hard-typed processing is **3.2× faster** than string processing.

## Results Location

After running benchmarks, find results in:

```
BenchmarkDotNet.Artifacts/
└── results/
    ├── *.md      ← Markdown tables (easy to read)
    ├── *.html    ← HTML reports (interactive)
    ├── *.csv     ← Raw data (for analysis)
    └── *-report-github.md  ← GitHub-formatted results
```

## Quick Analysis

### Check Zero Allocations

```bash
# On macOS/Linux:
grep "ProcessHardTypedIntegers" BenchmarkDotNet.Artifacts/results/*.md | grep "1000000"

# On Windows PowerShell:
Select-String "ProcessHardTypedIntegers" BenchmarkDotNet.Artifacts/results/*.md | Select-String "1000000"
```

Should show: `96 B` or similar small constant.

### Compare String vs Hard-Typed

```bash
# On macOS/Linux:
grep -E "(ProcessHardTypedIntegers|ProcessIntegerStrings)" BenchmarkDotNet.Artifacts/results/*.md

# Windows PowerShell:
Select-String "(ProcessHardTypedIntegers|ProcessIntegerStrings)" BenchmarkDotNet.Artifacts/results/*.md
```

Should show ~3× speed difference and ~250,000× allocation difference.

## Common Scenarios

### Scenario 1: CI/CD Integration

Add to your build pipeline:

```yaml
# GitHub Actions example
- name: Run benchmarks
  run: |
    cd benchmarks
    dotnet run -c Release -- quick
```

### Scenario 2: Performance Regression Testing

Compare before and after changes:

```bash
# Before changes
dotnet run -c Release -- allocations
cp -r BenchmarkDotNet.Artifacts/results results-before/

# Make changes...

# After changes
dotnet run -c Release -- allocations
cp -r BenchmarkDotNet.Artifacts/results results-after/

# Compare
diff results-before/*.md results-after/*.md
```

### Scenario 3: Hardware Comparison

Test on different machines:

```bash
# On each machine:
dotnet run -c Release -- quick AllocationBenchmarks > results-$(hostname).txt
```

Compare throughput and verify zero allocations across all hardware.

## Troubleshooting

### Build Fails

**Problem**: `error CS0246: The type or namespace name 'BenchmarkDotNet' could not be found`

**Solution**:
```bash
dotnet restore
dotnet build -c Release
```

### Benchmarks Take Too Long

**Problem**: Benchmarks running for hours

**Solution**: Use `quick` mode:
```bash
dotnet run -c Release -- quick
```

Or test specific methods:
```bash
dotnet run -c Release -- quick --filter "*HardTypedIntegers*"
```

### High Allocations Reported

**Problem**: Showing MB instead of bytes for hard-typed tests

**Possible Causes**:
1. Running Debug build (use `-c Release`)
2. Testing string processing by mistake
3. Benchmark setup converting to strings

**Check**:
```bash
# Verify Release build
dotnet build -c Release

# Review benchmark code
cat AllocationBenchmarks.cs | grep -A 10 "ProcessHardTypedIntegers"
```

### Results Inconsistent

**Problem**: Results vary significantly between runs

**Solutions**:
1. Close other applications
2. Disable real-time antivirus
3. Use AC power (laptops)
4. Let system cool down
5. Increase iteration count (takes longer)

## Next Steps

After quick start:

1. **Read [README.md](README.md)** - Comprehensive documentation
2. **Review [BENCHMARK_RESULTS.md](BENCHMARK_RESULTS.md)** - Expected results
3. **Customize benchmarks** - Add your own scenarios
4. **Share results** - Contribute back to the project

## Getting Help

- **Documentation**: See [README.md](README.md)
- **Issues**: Create issue on GitHub
- **Questions**: Discussion forum

## Tips for Best Results

### Before Running

- ✅ Close unnecessary applications
- ✅ Plug in laptop (disable power saving)
- ✅ Disable real-time antivirus temporarily
- ✅ Ensure adequate cooling
- ✅ Use Release build (`-c Release`)

### During Running

- ✅ Don't use the computer heavily
- ✅ Let benchmarks complete uninterrupted
- ✅ Monitor for thermal throttling
- ✅ Watch for error messages

### After Running

- ✅ Review outliers in results
- ✅ Check statistical significance
- ✅ Compare with expected results
- ✅ Document hardware configuration

## Minimal Example

Simplest possible benchmark run:

```bash
cd /path/to/TypeGuesser/benchmarks
dotnet run -c Release -- quick
```

That's it! Results in 5 minutes.

## Expected Time Investment

| Task                           | Time      | Value                              |
| ------------------------------ | --------- | ---------------------------------- |
| Setup & build                  | 1 min     | Verify environment                 |
| Quick validation               | 5 min     | Confirm benchmarks work            |
| Allocation benchmarks          | 15 min    | Prove zero-allocation claim        |
| Performance benchmarks         | 20 min    | Measure throughput                 |
| Thread-safety benchmarks       | 20 min    | Test concurrent patterns           |
| Full suite                     | 60 min    | Comprehensive validation           |
| Analysis & documentation       | 30 min    | Understand and document results    |

**Recommended first time**: Quick validation (5 min) → Allocation benchmarks (15 min) → Review results.

## Sample Session

```bash
$ cd benchmarks

$ dotnet build -c Release
Build succeeded.

$ dotnet run -c Release -- allocations
╔════════════════════════════════════════════════════════════════╗
║         TypeGuesser Performance Benchmark Suite                ║
║  Testing zero-allocation claims and performance characteristics ║
╚════════════════════════════════════════════════════════════════╝

Running ALLOCATION benchmarks only...

// Warmup 1: 5.234 ms
// Warmup 2: 5.189 ms
...
// Result 1: 5.234 ms, Allocated: 96 B
// Result 2: 5.241 ms, Allocated: 96 B
...

| Method                      | N       | Mean    | Allocated |
| --------------------------- | ------- | ------- | --------- |
| ProcessHardTypedIntegers    | 1000000 | 5.23 ms | 96 B      | ← Zero allocations!

Benchmarks Complete!
Results are available in:
  - BenchmarkDotNet.Artifacts/results/

Press any key to exit...
```

## One-Line Validation

Absolute quickest validation:

```bash
dotnet run -c Release -p benchmarks/TypeGuesser.Benchmarks.csproj -- quick --filter "*ProcessHardTypedIntegers*"
```

This runs ONLY the baseline zero-allocation test in ~2 minutes.

---

**You're ready!** Start with `quick` mode and work your way up to full benchmarks.
