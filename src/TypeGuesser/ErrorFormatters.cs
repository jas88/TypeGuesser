using System;

namespace TypeGuesser;

/// <summary>
/// Static error message formatters with compile-time type safety and optimal performance.
/// Replaces the ResourceManager-based approach with direct string interpolation.
/// </summary>
public static class ErrorFormatters
{
    /// <summary>
    /// Formats error message for unsupported types.
    /// </summary>
    public static string UnsupportedType(Type type) =>
        $"No Type Decider exists for Type:{type}";

    /// <summary>
    /// Formats error message for incompatible type combinations.
    /// </summary>
    public static string CannotCombineTypes(Type type1, Type type2) =>
        $"Could not combine Types '{type1}' and '{type2}' because they were of differing Types and neither Type appeared in the PreferenceOrder";

    /// <summary>
    /// Formats error message for DateTime parsing failures.
    /// </summary>
    public static string DateTimeParseError(string value) =>
        $"Could not parse '{value}' to a valid DateTime";

    /// <summary>
    /// Formats error message for string parsing failures.
    /// </summary>
    public static string StringParseError(string value, Type deciderType) =>
        $"Could not parse string value '{value}' with Decider Type:{deciderType.Name}";

    /// <summary>
    /// Formats error message for mixed typing scenarios in Guesser.
    /// </summary>
    public static string MixedTypingError(object value, Type valueType, Type previousType) =>
        $"Guesser does not support being passed hard typed objects (e.g. int) mixed with untyped objects (e.g. string).  We were adjusting to compensate for object '{value}' which is of Type '{valueType}', we were previously passed a '{previousType}' type";

    /// <summary>
    /// Formats error message for mixed typing in PooledBuilder (int after string).
    /// </summary>
    public static string MixedTypingIntAfterString() =>
        "Cannot process hard-typed int value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.";

    /// <summary>
    /// Formats error message for mixed typing in PooledBuilder (decimal after string).
    /// </summary>
    public static string MixedTypingDecimalAfterString() =>
        "Cannot process hard-typed decimal value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.";

    /// <summary>
    /// Formats error message for mixed typing in PooledBuilder (bool after string).
    /// </summary>
    public static string MixedTypingBoolAfterString() =>
        "Cannot process hard-typed bool value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.";

    /// <summary>
    /// Formats error message for mixed typing in PooledBuilder (generic type after string).
    /// </summary>
    public static string MixedTypingGenericTypeAfterString(Type type) =>
        $"Cannot process hard-typed {type} value after processing string values. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.";

    /// <summary>
    /// Formats error message for abstract base configuration errors.
    /// </summary>
    public static string AbstractBaseError() =>
        "DecideTypesForStrings abstract base was not passed any typesSupported by implementing derived class";
}