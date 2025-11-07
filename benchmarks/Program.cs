using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace TypeGuesser.Benchmarks;

/// <summary>
/// Benchmark runner for TypeGuesser performance analysis.
/// Generates comprehensive reports on allocations, throughput, and thread-safety.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         TypeGuesser Performance Benchmark Suite                ║");
        Console.WriteLine("║                                                                ║");
        Console.WriteLine("║  Testing zero-allocation claims and performance characteristics ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configure benchmark runner
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(CsvExporter.Default)
            .AddExporter(HtmlExporter.Default)
            .WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default.WithMaxParameterColumnWidth(50));

        if (args.Length > 0)
        {
            // Allow running specific benchmark classes
            switch (args[0].ToLowerInvariant())
            {
                case "allocations":
                case "alloc":
                    Console.WriteLine("Running ALLOCATION benchmarks only...");
                    Console.WriteLine("Focus: Proving zero-allocation performance for hard-typed inputs\n");
                    BenchmarkRunner.Run<AllocationBenchmarks>(config);
                    break;

                case "performance":
                case "perf":
                    Console.WriteLine("Running PERFORMANCE benchmarks only...");
                    Console.WriteLine("Focus: Throughput, speed, and configuration comparisons\n");
                    BenchmarkRunner.Run<PerformanceBenchmarks>(config);
                    break;

                case "threads":
                case "thread":
                    Console.WriteLine("Running THREAD-SAFETY benchmarks only...");
                    Console.WriteLine("Focus: Concurrent usage patterns and contention scenarios\n");
                    BenchmarkRunner.Run<ThreadSafetyBenchmarks>(config);
                    break;

                case "quick":
                    Console.WriteLine("Running QUICK benchmarks (small datasets only)...");
                    Console.WriteLine("Focus: Fast validation of all benchmark categories\n");
                    var quickConfig = config
                        .AddJob(Job.Default
                            .WithToolchain(InProcessEmitToolchain.Instance)
                            .WithWarmupCount(1)
                            .WithIterationCount(3));
                    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args.Skip(1).ToArray(), quickConfig);
                    break;

                default:
                    Console.WriteLine($"Unknown benchmark category: {args[0]}");
                    Console.WriteLine("Available options:");
                    Console.WriteLine("  allocations - Test memory allocation patterns");
                    Console.WriteLine("  performance - Test throughput and speed");
                    Console.WriteLine("  threads     - Test concurrent usage patterns");
                    Console.WriteLine("  quick       - Fast validation run");
                    Console.WriteLine("  (no args)   - Run all benchmarks");
                    return;
            }
        }
        else
        {
            // Run all benchmarks
            Console.WriteLine("Running ALL benchmarks...");
            Console.WriteLine("This will take significant time. Use 'quick' for faster validation.\n");

            PrintBenchmarkInfo();

            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    Benchmarks Complete!                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Results are available in:");
            Console.WriteLine("  - BenchmarkDotNet.Artifacts/results/ (detailed reports)");
            Console.WriteLine("  - *.md files (GitHub-formatted markdown)");
            Console.WriteLine("  - *.html files (HTML reports)");
            Console.WriteLine("  - *.csv files (CSV data for analysis)");
            Console.WriteLine();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void PrintBenchmarkInfo()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  ALLOCATION BENCHMARKS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Purpose: Prove zero-allocation performance claims");
        Console.WriteLine();
        Console.WriteLine("Tests:");
        Console.WriteLine("  ✓ Hard-typed integers (ZERO allocation target)");
        Console.WriteLine("  ✓ Hard-typed decimals (ZERO allocation target)");
        Console.WriteLine("  ✓ Hard-typed DateTimes (ZERO allocation target)");
        Console.WriteLine("  ✓ String processing (baseline with allocations)");
        Console.WriteLine("  ✓ Mixed types with fallback scenarios");
        Console.WriteLine("  ✓ CSV-like scenarios with nulls");
        Console.WriteLine();
        Console.WriteLine("Expected Results:");
        Console.WriteLine("  - Gen0/Gen1/Gen2 collections: 0 for hard-typed paths");
        Console.WriteLine("  - Allocated memory: ~96 bytes (Guesser instance only)");
        Console.WriteLine("  - String paths: Show baseline allocation costs");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  PERFORMANCE BENCHMARKS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Purpose: Measure throughput and speed characteristics");
        Console.WriteLine();
        Console.WriteLine("Tests:");
        Console.WriteLine("  ✓ Throughput for all data types");
        Console.WriteLine("  ✓ Culture-specific processing");
        Console.WriteLine("  ✓ Parse performance");
        Console.WriteLine("  ✓ Settings configuration impact");
        Console.WriteLine("  ✓ Real-world scenarios (DataTable, CSV, fallback)");
        Console.WriteLine();
        Console.WriteLine("Expected Results:");
        Console.WriteLine("  - Hard-typed: 2-3× faster than string parsing");
        Console.WriteLine("  - Scale linearly with dataset size");
        Console.WriteLine("  - Culture impact: Minimal for hard-typed, moderate for strings");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  THREAD-SAFETY BENCHMARKS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Purpose: Test concurrent usage patterns and scalability");
        Console.WriteLine();
        Console.WriteLine("Tests:");
        Console.WriteLine("  ✓ Thread-local instances (recommended pattern)");
        Console.WriteLine("  ✓ Object pooling");
        Console.WriteLine("  ✓ Partitioned batch processing");
        Console.WriteLine("  ✓ Work stealing");
        Console.WriteLine("  ✓ Multi-column DataTable processing");
        Console.WriteLine("  ✓ Parallel CSV chunk processing");
        Console.WriteLine("  ✓ Producer-consumer pattern");
        Console.WriteLine("  ⚠ Shared instance with lock (anti-pattern for comparison)");
        Console.WriteLine();
        Console.WriteLine("Expected Results:");
        Console.WriteLine("  - Thread-local: Best performance, zero contention");
        Console.WriteLine("  - Shared with lock: Worst performance (demonstrates why not to do this)");
        Console.WriteLine("  - Pooling: Moderate benefit for high-allocation scenarios");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
