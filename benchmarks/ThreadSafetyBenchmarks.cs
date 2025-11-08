using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Collections.Concurrent;
using TypeGuesser;

namespace TypeGuesser.Benchmarks;

/// <summary>
/// Benchmarks focused on thread-safety and concurrent usage patterns.
/// Tests different approaches for using Guesser in multi-threaded scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 10)]
public class ThreadSafetyBenchmarks
{
    private int[][] _partitionedIntegers = null!;
    private string[][] _partitionedStrings = null!;
    private decimal[][] _partitionedDecimals = null!;

    [Params(4, 8, 16)]
    public int ThreadCount;

    [Params(10_000, 100_000)]
    public int ItemsPerThread;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);

        // Create partitioned datasets for parallel processing
        _partitionedIntegers = new int[ThreadCount][];
        _partitionedStrings = new string[ThreadCount][];
        _partitionedDecimals = new decimal[ThreadCount][];

        for (var t = 0; t < ThreadCount; t++)
        {
            _partitionedIntegers[t] = new int[ItemsPerThread];
            _partitionedStrings[t] = new string[ItemsPerThread];
            _partitionedDecimals[t] = new decimal[ItemsPerThread];

            for (var i = 0; i < ItemsPerThread; i++)
            {
                _partitionedIntegers[t][i] = random.Next(-1000000, 1000000);
                _partitionedStrings[t][i] = _partitionedIntegers[t][i].ToString();
                _partitionedDecimals[t][i] = (decimal)(random.NextDouble() * 100000);
            }
        }
    }

    #region Pattern 1: Thread-Local Guesser Instances

    /// <summary>
    /// RECOMMENDED: Each thread creates its own Guesser instance.
    /// No synchronization needed, maximum parallelism, zero contention.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Pattern: Thread-local Guesser instances")]
    public DatabaseTypeRequest[] ThreadLocalInstances()
    {
        var results = new DatabaseTypeRequest[ThreadCount];

        Parallel.For(0, ThreadCount, threadIndex =>
        {
            var guesser = new Guesser();
            foreach (var value in _partitionedIntegers[threadIndex])
            {
                guesser.AdjustToCompensateForValue(value);
            }
            results[threadIndex] = guesser.Guess;
        });

        return results;
    }

    /// <summary>
    /// Thread-local instances with string processing.
    /// </summary>
    [Benchmark(Description = "Pattern: Thread-local with string processing")]
    public DatabaseTypeRequest[] ThreadLocalStrings()
    {
        var results = new DatabaseTypeRequest[ThreadCount];

        Parallel.For(0, ThreadCount, threadIndex =>
        {
            var guesser = new Guesser();
            foreach (var value in _partitionedStrings[threadIndex])
            {
                guesser.AdjustToCompensateForValue(value);
            }
            results[threadIndex] = guesser.Guess;
        });

        return results;
    }

    #endregion

    #region Pattern 2: Object Pooling

    /// <summary>
    /// Using a pool of Guesser instances to reduce allocations.
    /// Demonstrates pooling overhead vs allocation savings.
    /// </summary>
    [Benchmark(Description = "Pattern: Object pooling with ConcurrentBag")]
    public DatabaseTypeRequest[] ObjectPooling()
    {
        // Create a simple pool
        var pool = new ConcurrentBag<Guesser>();
        for (var i = 0; i < ThreadCount; i++)
        {
            pool.Add(new Guesser());
        }

        var results = new DatabaseTypeRequest[ThreadCount];

        Parallel.For(0, ThreadCount, threadIndex =>
        {
            // Rent from pool
            if (!pool.TryTake(out var guesser))
            {
                guesser = new Guesser();
            }

            foreach (var value in _partitionedIntegers[threadIndex])
            {
                guesser.AdjustToCompensateForValue(value);
            }
            results[threadIndex] = guesser.Guess;

            // Return to pool (in real usage, you'd create a new instance)
            // pool.Add(new Guesser());
        });

        return results;
    }

    #endregion

    #region Pattern 3: Partitioned Processing

    /// <summary>
    /// Process data in chunks, one Guesser per chunk.
    /// Simulates batch processing scenarios.
    /// </summary>
    [Benchmark(Description = "Pattern: Partitioned batch processing")]
    public DatabaseTypeRequest[] PartitionedBatches()
    {
        var batchSize = 1000;
        var results = new ConcurrentBag<DatabaseTypeRequest>();

        Parallel.ForEach(Partitioner.Create(0, ThreadCount * ItemsPerThread, batchSize), range =>
        {
            var guesser = new Guesser();
            var threadIndex = range.Item1 / ItemsPerThread;
            var startIndex = range.Item1 % ItemsPerThread;
            var endIndex = Math.Min(range.Item2 % ItemsPerThread, ItemsPerThread);

            if (threadIndex >= ThreadCount) return;

            for (var i = startIndex; i < endIndex; i++)
            {
                guesser.AdjustToCompensateForValue(_partitionedIntegers[threadIndex][i]);
            }
            results.Add(guesser.Guess);
        });

        return results.ToArray();
    }

    #endregion

    #region Pattern 4: Work Stealing

    /// <summary>
    /// Threads process work items from a shared queue.
    /// Tests concurrent queue operations with Guesser processing.
    /// </summary>
    [Benchmark(Description = "Pattern: Work stealing from concurrent queue")]
    public DatabaseTypeRequest[] WorkStealing()
    {
        var workQueue = new ConcurrentQueue<int[]>();

        // Enqueue all work
        for (var t = 0; t < ThreadCount; t++)
        {
            workQueue.Enqueue(_partitionedIntegers[t]);
        }

        var results = new ConcurrentBag<DatabaseTypeRequest>();

        Parallel.For(0, ThreadCount, _ =>
        {
            while (workQueue.TryDequeue(out var work))
            {
                var guesser = new Guesser();
                foreach (var value in work)
                {
                    guesser.AdjustToCompensateForValue(value);
                }
                results.Add(guesser.Guess);
            }
        });

        return results.ToArray();
    }

    #endregion

    #region Contention Scenarios

    /// <summary>
    /// ANTI-PATTERN: Shared Guesser with locking.
    /// Demonstrates why you should NOT share Guesser instances.
    /// Included for comparison to show the cost of synchronization.
    /// </summary>
    [Benchmark(Description = "Anti-pattern: Shared Guesser with lock (shows contention)")]
    public DatabaseTypeRequest SharedGuesserWithLock()
    {
        var guesser = new Guesser();
        var lockObject = new object();

        Parallel.For(0, ThreadCount, threadIndex =>
        {
            foreach (var value in _partitionedIntegers[threadIndex])
            {
                lock (lockObject)
                {
                    guesser.AdjustToCompensateForValue(value);
                }
            }
        });

        return guesser.Guess;
    }

    /// <summary>
    /// Sequential baseline for comparison.
    /// </summary>
    [Benchmark(Description = "Baseline: Sequential processing")]
    public DatabaseTypeRequest SequentialProcessing()
    {
        var guesser = new Guesser();

        for (var t = 0; t < ThreadCount; t++)
        {
            foreach (var value in _partitionedIntegers[t])
            {
                guesser.AdjustToCompensateForValue(value);
            }
        }

        return guesser.Guess;
    }

    #endregion

    #region Real-World Multi-Column Scenarios

    /// <summary>
    /// Simulates processing multiple DataTable columns in parallel.
    /// Each column gets its own Guesser, processed independently.
    /// </summary>
    [Benchmark(Description = "Scenario: Multi-column DataTable processing")]
    public DatabaseTypeRequest[] MultiColumnDataTable()
    {
        var columnCount = ThreadCount;
        var results = new DatabaseTypeRequest[columnCount];

        // Each column processed in parallel with its own Guesser
        Parallel.For(0, columnCount, columnIndex =>
        {
            var guesser = new Guesser();

            // Process all rows for this column
            foreach (var value in _partitionedStrings[columnIndex])
            {
                guesser.AdjustToCompensateForValue(value);
            }

            results[columnIndex] = guesser.Guess;
        });

        return results;
    }

    /// <summary>
    /// Simulates parallel CSV file processing.
    /// Each thread processes a separate file chunk.
    /// </summary>
    [Benchmark(Description = "Scenario: Parallel CSV chunk processing")]
    public DatabaseTypeRequest[] ParallelCsvChunks()
    {
        var results = new DatabaseTypeRequest[ThreadCount];

        Parallel.For(0, ThreadCount, chunkIndex =>
        {
            // Each chunk has its own Guesser
            var guesser = new Guesser();

            foreach (var value in _partitionedStrings[chunkIndex])
            {
                guesser.AdjustToCompensateForValue(value);
            }

            results[chunkIndex] = guesser.Guess;
        });

        return results;
    }

    /// <summary>
    /// Simulates streaming data processing with producer-consumer pattern.
    /// </summary>
    [Benchmark(Description = "Scenario: Producer-consumer pattern")]
    public DatabaseTypeRequest[] ProducerConsumer()
    {
        using (var channel = new BlockingCollection<int>(boundedCapacity: 1000))
        {
            var results = new ConcurrentBag<DatabaseTypeRequest>();

            // Producer task
            var producer = Task.Run(() =>
            {
                for (var t = 0; t < ThreadCount; t++)
                {
                    foreach (var value in _partitionedIntegers[t])
                    {
                        channel.Add(value);
                    }
                }
                channel.CompleteAdding();
            });

            // Consumer tasks
            var consumers = Enumerable.Range(0, ThreadCount / 2).Select(_ => Task.Run(() =>
            {
                var guesser = new Guesser();
                var count = 0;
                var batchSize = 1000;

                foreach (var value in channel.GetConsumingEnumerable())
                {
                    guesser.AdjustToCompensateForValue(value);
                    count++;

                    // Emit result every batchSize items
                    if (count >= batchSize)
                    {
                        results.Add(guesser.Guess);
                        guesser = new Guesser();
                        count = 0;
                    }
                }

                // Emit final result
                if (count > 0)
                {
                    results.Add(guesser.Guess);
                }
            })).ToArray();

            Task.WaitAll(consumers);
            producer.Wait();

            return results.ToArray();
        }
    }

    #endregion
}
