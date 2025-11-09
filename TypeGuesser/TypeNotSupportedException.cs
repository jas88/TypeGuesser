using System;

namespace TypeGuesser;

/// <summary>
/// Thrown when a given Type is not supported by TypeGuesser
/// </summary>
public sealed class TypeNotSupportedException(Type t) : Exception($"DataType {t} does not have an associated IDecideTypesForStrings");