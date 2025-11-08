using System;
using System.Globalization;

namespace TypeGuesser;

/// <summary>
/// Example usage of the new pooled builder infrastructure.
/// This is not part of the public API but serves as documentation.
/// </summary>
internal static class PooledBuilderExample
{
    /// <summary>
    /// Example: Using the pooled builder for zero-allocation processing of hard-typed values
    /// </summary>
    public static TypeGuessResult ExampleHardTypedValues()
    {
        var builder = TypeGuesserBuilderPool.Rent(CultureInfo.InvariantCulture);
        try
        {
            // Zero-allocation processing for hard-typed values
            builder.ProcessIntZeroAlloc(42);
            builder.ProcessIntZeroAlloc(100);
            builder.ProcessIntZeroAlloc(-5);

            return builder.Build();
        }
        finally
        {
            TypeGuesserBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Example: Using the pooled builder for decimal processing
    /// </summary>
    public static TypeGuessResult ExampleDecimalValues()
    {
        var builder = TypeGuesserBuilderPool.Rent();
        try
        {
            builder.ProcessDecimalZeroAlloc(3.14159m);
            builder.ProcessDecimalZeroAlloc(2.71828m);
            builder.ProcessDecimalZeroAlloc(1.41421m);

            return builder.Build();
        }
        finally
        {
            TypeGuesserBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Example: Using the pooled builder for string processing
    /// </summary>
    public static TypeGuessResult ExampleStringValues()
    {
        var builder = TypeGuesserBuilderPool.Rent();
        try
        {
            builder.ProcessString("123".AsSpan());
            builder.ProcessString("456".AsSpan());
            builder.ProcessString("789".AsSpan());

            return builder.Build();
        }
        finally
        {
            TypeGuesserBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Example: Using the pooled builder with mixed types (will fall back to string)
    /// </summary>
    public static TypeGuessResult ExampleMixedValues()
    {
        var builder = TypeGuesserBuilderPool.Rent();
        try
        {
            builder.ProcessString("123".AsSpan());
            builder.ProcessString("not a number".AsSpan());
            builder.ProcessString("456".AsSpan());

            return builder.Build();
        }
        finally
        {
            TypeGuesserBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Example: Converting result to DatabaseTypeRequest for backward compatibility
    /// </summary>
    public static DatabaseTypeRequest ExampleBackwardCompatibility()
    {
        var builder = TypeGuesserBuilderPool.Rent();
        try
        {
            builder.ProcessString("2024-01-01".AsSpan());
            builder.ProcessString("2024-12-31".AsSpan());

            var result = builder.Build();
            return result.ToDatabaseTypeRequest();
        }
        finally
        {
            TypeGuesserBuilderPool.Return(builder);
        }
    }
}
