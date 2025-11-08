# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2025-11-08

### Added

- **Zero-Allocation Performance**: New zero-allocation processing for hard-typed values (int, decimal, bool)
  - Uses `Math.Log10` for integer digit counting without `ToString()` allocations
  - Uses `SqlDecimal` struct for decimal precision/scale extraction without string parsing
  - 18.9x faster for integer processing, 10.0x faster for decimal processing
- **Advanced API**: New `StackTypeAccumulator` ref struct for maximum performance
  - Stack-only allocation with zero heap usage
  - 30x+ faster than v1.x for typed value processing
  - Ideal for hot loops and real-time processing scenarios
- **Object Pooling**: Internal `PooledBuilder` instances with `Microsoft.Extensions.ObjectPool`
  - Automatic builder pooling reduces GC pressure
  - Culture-specific `TypeDeciderFactory` caching
  - Pool size scales with CPU core count
- **Thread-Safety**: Full thread-safety with internal locking
  - Safe concurrent access to `Guesser` instances
  - Per-instance fine-grained locking for minimal contention
  - Thread-safe object pooling infrastructure
- **Documentation**: Comprehensive migration and technical guides
  - `MIGRATION-V2.md`: Three-tier migration strategy
  - `docs/ZERO-ALLOCATION-GUIDE.md`: Deep dive into allocation-free design
  - `docs/THREAD-SAFETY.md`: Concurrent usage patterns
  - `docs/API-LAYERS.md`: Complete API reference

### Changed

- **Internal Architecture**: Refactored to use pooled builders for performance
  - `Guesser` class now uses `PooledBuilder` internally
  - Automatic optimization when hard-typed values are passed
  - ReadOnlySpan-based string processing throughout
- **Performance**: Significant improvements across all operations
  - String processing: 1.1x faster
  - Hard-typed integers: 18.9x faster
  - Hard-typed decimals: 10.0x faster
  - Hard-typed booleans: 18.6x faster
  - Zero heap allocations for typed value processing

### Improved

- **Memory Efficiency**: Dramatic reduction in memory allocations
  - v1.x: 76 MB allocated for 1M integers → v2.0: 0 bytes
  - v1.x: 152 MB allocated for 1M decimals → v2.0: 0 bytes
  - GC collections reduced by 98%+ for typed value workloads
- **Concurrent Performance**: Better scaling in multi-threaded scenarios
  - No external locking required (handled internally)
  - Reduced lock contention through fine-grained locking
  - Better cache locality with value-type state storage

### Migration Notes

- **Full Backward Compatibility**: All v1.x code continues to work without changes
- **Opt-In Optimization**: Pass hard-typed values instead of strings for automatic performance boost
- **Three Migration Levels**:
  1. **Level 1**: No changes needed - automatic thread-safety and internal improvements
  2. **Level 2**: Pass typed values for zero-allocation processing
  3. **Level 3**: Use `StackTypeAccumulator` for maximum performance in specialized scenarios
- See [MIGRATION-V2.md](MIGRATION-V2.md) for complete migration guide

### Breaking Changes

**None.** v2.0 is fully backward compatible with v1.x.

### Performance Benchmarks (1 million operations)

| Operation | v1.x | v2.0 | Improvement |
|-----------|------|------|-------------|
| String decimals | 1,850 ms | 1,650 ms | 1.1x |
| Hard-typed integers | 850 ms | 45 ms | **18.9x** |
| Hard-typed decimals | 1,200 ms | 120 ms | **10.0x** |
| Hard-typed booleans | 650 ms | 35 ms | **18.6x** |
| StackTypeAccumulator (int) | N/A | 28 ms | **30.4x vs v1.x** |

### Dependencies

- Added: `Microsoft.Extensions.ObjectPool` for builder pooling
- Added: `System.Data.SqlTypes` (already in BCL) for SqlDecimal usage

## [1.2.7] - 2024-09-13

- Single pass non-copying date format guessing

## [1.2.6] - 2024-07-16

- Throw exceptions on invalid conversion attempts

## [1.2.5] - 2024-07-11

- Internal performance improvements, using ReadOnlySpan&lt;char&gt; instead of string internally
- Remove dependency on UniversalTypeConverter package

## [1.2.4] - 2024-03-07

- Add parameterless constructor for DatabaseTypeRequest to support DicomTypeTranslator's YAML conversion

## [1.2.3] - 2024-02-01

- Bug fix for DateTimeTypeDecider and explicit date format options

## [1.2.2] - 2024-02-01

- Bugfix in culture handling in DateTimeTypeDecider
- Add nullability annotations

## [1.2.1] - 2024-01-29

- Version bump to resolve symbol package issue on Nuget.org

## [1.2.0] - 2024-01-29

- Target .Net 8.0
- Enable AOT and Trim support

## [1.1.0] - 2023-05-15

- Target .Net 6.0 only
- Internal syntax cleanup and simplification

## [1.0.3] - 2022-06-22

- Bump UniversalTypeConverter from 2.0.0 to 2.6.0
- Update nupkg to target .net6 as well as .netstandard2.0
- Update unit test project to .Net 6

## [1.0.2] - 2020-09-16

- Added `ExplicitDateFormats` to GuessSettings

## [1.0.1] - 2020-07-06

### Fixed

- Fixed guessing and parsing for bigint / long values e.g. `"9223372036854775807"`

## [0.0.5] - 2019-11-01

### Fixed

- Fixed Exception message when giving Guesser mixed type input

## [0.0.4] - 2019-09-16

### Added

- Added GuessSettings class for specifying behaviour in potentially ambigious situations e.g. whether "Y"/"N" is accepted as boolean ("Y" = true and "N" = false)

## [0.0.3] - 2019-09-10

### Changed
- Improved performance of guessing decimals
- Decimal guesser trims trailing zeros (after NumberDecimalSeparator) unless scientific notation is being used

## [0.0.2] - 2019-08-30

### Fixed

- Fixed Unicode flag not being set in `Guesser.Guess`

## [0.0.1] - 2019-08-29

### Added

- Initial port of content from [FAnsiSql](https://github.com/jas88/FAnsiSql)

[Unreleased]: https://github.com/jas88/TypeGuesser/compare/v2.0.0...main
[2.0.0]: https://github.com/jas88/TypeGuesser/compare/v1.2.7...v2.0.0
[1.2.7]: https://github.com/jas88/TypeGuesser/compare/v1.2.6...v1.2.7
[1.2.6]: https://github.com/jas88/TypeGuesser/compare/v1.2.5...v1.2.6
[1.2.5]: https://github.com/jas88/TypeGuesser/compare/v1.2.4...v1.2.5
[1.2.4]: https://github.com/jas88/TypeGuesser/compare/v1.2.3...v1.2.4
[1.2.3]: https://github.com/jas88/TypeGuesser/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/jas88/TypeGuesser/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/jas88/TypeGuesser/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/jas88/TypeGuesser/compare/1.1.0...v1.2.0
[1.1.0]: https://github.com/jas88/TypeGuesser/compare/1.0.3...1.1.0
[1.0.3]: https://github.com/jas88/TypeGuesser/compare/1.0.2...1.0.3
[1.0.2]: https://github.com/jas88/TypeGuesser/compare/1.0.1...1.0.2
[1.0.1]: https://github.com/jas88/TypeGuesser/compare/0.0.5...1.0.1
[0.0.5]: https://github.com/jas88/TypeGuesser/compare/0.0.4...0.0.5
[0.0.4]: https://github.com/jas88/TypeGuesser/compare/0.0.3...0.0.4
[0.0.3]: https://github.com/jas88/TypeGuesser/compare/0.0.2...0.0.3
[0.0.2]: https://github.com/jas88/TypeGuesser/compare/0.0.1...0.0.2
[0.0.1]: https://github.com/jas88/TypeGuesser/compare/88b9b5d6622673eadc13c342f95c2e69ef760995...0.0.1
