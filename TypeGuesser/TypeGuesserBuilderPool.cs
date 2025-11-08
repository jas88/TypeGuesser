using System;
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.ObjectPool;

namespace TypeGuesser;

/// <summary>
/// Static pool manager for <see cref="PooledBuilder"/> instances, providing thread-safe
/// object pooling with culture-specific type decider caching.
/// </summary>
/// <remarks>
/// This class manages a pool of reusable <see cref="PooledBuilder"/> instances and caches
/// <see cref="TypeDeciderFactory"/> instances per culture for optimal performance.
/// The pool uses Microsoft.Extensions.ObjectPool.
/// </remarks>
internal static class TypeGuesserBuilderPool
{
    private static readonly ObjectPool<PooledBuilder> _pool;
    private static readonly ConcurrentDictionary<CultureInfo, TypeDeciderFactory> _deciderFactoryCache = new();

    static TypeGuesserBuilderPool()
    {
        var provider = new DefaultObjectPoolProvider
        {
            MaximumRetained = Environment.ProcessorCount * 2
        };
        _pool = provider.Create(new PooledBuilderPolicy());
    }

    /// <summary>
    /// Rents a <see cref="PooledBuilder"/> from the pool, configured with the specified culture.
    /// </summary>
    /// <param name="culture">The culture to use for type parsing. If null, uses <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>A pooled builder ready for use. Call <see cref="Return"/> when finished.</returns>
    /// <remarks>
    /// The returned builder is reset and ready for use. You must call <see cref="Return"/>
    /// when finished to return it to the pool for reuse.
    /// </remarks>
    internal static PooledBuilder Rent(CultureInfo? culture = null)
    {
        var builder = _pool.Get();
        var targetCulture = culture ?? CultureInfo.CurrentCulture;

        // Update culture if different from current
        if (!Equals(builder.Culture, targetCulture))
        {
            builder.SetCulture(targetCulture);
        }

        return builder;
    }

    /// <summary>
    /// Returns a <see cref="PooledBuilder"/> to the pool for reuse.
    /// </summary>
    /// <param name="builder">The builder to return. Must not be null.</param>
    /// <remarks>
    /// After calling this method, do not use the builder instance anymore.
    /// The builder will be reset and may be rented by another caller.
    /// </remarks>
    internal static void Return(PooledBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        _pool.Return(builder);
    }

    /// <summary>
    /// Gets or creates a cached <see cref="TypeDeciderFactory"/> for the specified culture.
    /// </summary>
    /// <param name="culture">The culture to get the factory for</param>
    /// <returns>A cached or newly created type decider factory</returns>
    internal static TypeDeciderFactory GetOrCreateDeciderFactory(CultureInfo culture)
    {
        return _deciderFactoryCache.GetOrAdd(
            culture ?? CultureInfo.CurrentCulture,
            c => new TypeDeciderFactory(c));
    }

    /// <summary>
    /// Clears all cached type decider factories. Useful for testing or memory cleanup.
    /// </summary>
    internal static void ClearCache()
    {
        _deciderFactoryCache.Clear();
    }

    /// <summary>
    /// Object pool policy for creating and resetting <see cref="PooledBuilder"/> instances.
    /// </summary>
    private sealed class PooledBuilderPolicy : IPooledObjectPolicy<PooledBuilder>
    {
        /// <summary>
        /// Creates a new <see cref="PooledBuilder"/> instance for the pool.
        /// </summary>
        /// <returns>A new pooled builder with default culture</returns>
        public PooledBuilder Create()
        {
            return new PooledBuilder(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Resets a <see cref="PooledBuilder"/> when it's returned to the pool.
        /// </summary>
        /// <param name="obj">The builder to reset</param>
        /// <returns>True to return the object to the pool, false to discard it</returns>
        public bool Return(PooledBuilder obj)
        {
            if (obj == null)
            {
                return false;
            }

            obj.Reset();
            return true;
        }
    }
}
