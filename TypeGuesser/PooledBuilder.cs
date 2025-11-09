using System;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;

namespace TypeGuesser;

/// <summary>
/// Pooled builder for type guessing that supports zero-allocation processing
/// for hard-typed values (int, decimal, bool) and thread-safe operation via locking.
/// </summary>
/// <remarks>
/// This builder is designed for reuse via object pooling. After calling <see cref="Build"/>,
/// call <see cref="Reset"/> to prepare the instance for the next guessing operation.
/// All public methods are thread-safe via internal locking.
/// </remarks>
public sealed class PooledBuilder
{
    private readonly object _lock = new();
    private TypeDeciderFactory _typeDeciders;
    private CultureInfo _culture;

    // Current state - all value types for performance
    private Type _currentType;
    private int? _maxWidth;
    private int _digitsBeforeDecimal;
    private int _digitsAfterDecimal;
    private bool _requiresUnicode;
    private int _valueCount;
    private int _nullCount;
    private bool _isPrimedWithBonafideType;
    private TypeCompatibilityGroup _validTypesSeen;

    /// <summary>
    /// Configuration for extra length per non-ASCII character (Oracle compatibility).
    /// </summary>
    public int ExtraLengthPerNonAsciiCharacter { get; set; }

    /// <summary>
    /// The culture used for type parsing and decisions.
    /// </summary>
    public CultureInfo Culture
    {
        get
        {
            lock (_lock)
            {
                return _culture;
            }
        }
    }

    /// <summary>
    /// Instance-specific settings for type guessing behavior.
    /// </summary>
    public GuessSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return _typeDeciders.Settings;
            }
        }
    }

    /// <summary>
    /// Creates a new pooled builder with the specified culture.
    /// </summary>
    /// <param name="culture">The culture to use for type parsing</param>
    public PooledBuilder(CultureInfo culture)
    {
        _culture = culture ?? CultureInfo.CurrentCulture;
        _typeDeciders = new TypeDeciderFactory(_culture);
        _currentType = DatabaseTypeRequest.PreferenceOrder[0];
        Reset();
    }

    /// <summary>
    /// Resets the builder to its initial state, ready for reuse from the pool.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _currentType = DatabaseTypeRequest.PreferenceOrder[0];
            _maxWidth = null;
            _digitsBeforeDecimal = 0;
            _digitsAfterDecimal = 0;
            _requiresUnicode = false;
            _valueCount = 0;
            _nullCount = 0;
            _isPrimedWithBonafideType = false;
            _validTypesSeen = TypeCompatibilityGroup.None;
        }
    }

    /// <summary>
    /// Sets the initial type hint without processing any values.
    /// Used when constructing a Guesser with a specific DatabaseTypeRequest.
    /// </summary>
    /// <param name="type">The type to set as the initial guess</param>
    public void SetInitialTypeHint(Type type)
    {
        lock (_lock)
        {
            if (DatabaseTypeRequest.PreferenceOrder.Contains(type))
            {
                _currentType = type;
            }
        }
    }

    /// <summary>
    /// Updates the culture and reinitializes the type decider factory.
    /// </summary>
    /// <param name="culture">The new culture to use</param>
    public void SetCulture(CultureInfo culture)
    {
        lock (_lock)
        {
            _culture = culture ?? CultureInfo.CurrentCulture;
            _typeDeciders = new TypeDeciderFactory(_culture);
        }
    }

    /// <summary>
    /// Builds the final <see cref="TypeGuessResult"/> from the accumulated state.
    /// This method is thread-safe and returns a value type for zero allocations.
    /// </summary>
    /// <returns>The final type guess result</returns>
    public TypeGuessResult Build()
    {
        lock (_lock)
        {
            // Calculate final width considering decimal size if needed
            var finalWidth = _maxWidth;
            if (finalWidth.HasValue)
            {
                var decimalStringLength = CalculateDecimalStringLength();
                if (decimalStringLength > 0)
                {
                    finalWidth = Math.Max(finalWidth.Value, decimalStringLength);
                }
            }

            // For DateTime, ensure minimum width
            if (_currentType == typeof(DateTime) && finalWidth.HasValue)
            {
                finalWidth = Math.Max(finalWidth.Value, Guesser.MinimumLengthRequiredForDateStringRepresentation);
            }

            return new TypeGuessResult(
                _currentType,
                finalWidth,
                _digitsBeforeDecimal,
                _digitsAfterDecimal,
                _requiresUnicode,
                _valueCount,
                _nullCount);
        }
    }

    /// <summary>
    /// Processes a hard-typed integer value with zero allocations using Math.Log10.
    /// </summary>
    /// <param name="value">The integer value to process</param>
    public void ProcessIntZeroAlloc(int value)
    {
        lock (_lock)
        {
            // Check for mixing hard type with strings
            if (_validTypesSeen != TypeCompatibilityGroup.None || _currentType == typeof(string))
            {
                throw new MixedTypingException(ErrorFormatters.MixedTypingIntAfterString());
            }

            _valueCount++;

            // Prime type if needed
            if (!_isPrimedWithBonafideType)
            {
                _currentType = typeof(int);
                _isPrimedWithBonafideType = true;
            }
            else if (_currentType != typeof(int))
            {
                throw new MixedTypingException(
                    $"Cannot process int value when already primed with type {_currentType}. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.");
            }

            // Calculate digits without ToString() - zero allocation approach
            var digits = value == 0 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs((double)value))) + 1;
            _digitsBeforeDecimal = Math.Max(_digitsBeforeDecimal, digits);
            _digitsAfterDecimal = 0;

            // Update width for string representation
            var stringLength = digits + (value < 0 ? 1 : 0);
            _maxWidth = Math.Max(_maxWidth ?? 0, stringLength);
        }
    }

    /// <summary>
    /// Processes a hard-typed decimal value with zero allocations using SqlDecimal.
    /// </summary>
    /// <param name="value">The decimal value to process</param>
    public void ProcessDecimalZeroAlloc(decimal value)
    {
        lock (_lock)
        {
            // Check for mixing hard type with strings
            if (_validTypesSeen != TypeCompatibilityGroup.None || _currentType == typeof(string))
            {
                throw new MixedTypingException(ErrorFormatters.MixedTypingDecimalAfterString());
            }

            _valueCount++;

            // Prime type if needed
            if (!_isPrimedWithBonafideType)
            {
                _currentType = typeof(decimal);
                _isPrimedWithBonafideType = true;
            }
            else if (_currentType != typeof(decimal))
            {
                throw new MixedTypingException(
                    $"Cannot process decimal value when already primed with type {_currentType}. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.");
            }

            // Use SqlDecimal for zero-allocation precision/scale calculation
            var sqlDecimal = (SqlDecimal)value;
            var before = sqlDecimal.Precision - sqlDecimal.Scale;
            var after = sqlDecimal.Scale;

            _digitsBeforeDecimal = Math.Max(_digitsBeforeDecimal, before);
            _digitsAfterDecimal = Math.Max(_digitsAfterDecimal, after);

            // Update width for string representation
            var stringLength = value.ToString(_culture).Length;
            _maxWidth = Math.Max(_maxWidth ?? 0, stringLength);
        }
    }

    /// <summary>
    /// Processes a hard-typed boolean value with zero allocations.
    /// </summary>
    /// <param name="value">The boolean value to process</param>
    public void ProcessBoolZeroAlloc(bool value)
    {
        lock (_lock)
        {
            // Check for mixing hard type with strings
            if (_validTypesSeen != TypeCompatibilityGroup.None || _currentType == typeof(string))
            {
                throw new MixedTypingException(ErrorFormatters.MixedTypingBoolAfterString());
            }

            _valueCount++;

            // Prime type if needed
            if (!_isPrimedWithBonafideType)
            {
                _currentType = typeof(bool);
                _isPrimedWithBonafideType = true;
            }
            else if (_currentType != typeof(bool))
            {
                throw new MixedTypingException(
                    $"Cannot process bool value when already primed with type {_currentType}. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.");
            }

            // Boolean string representation: "True" (4) or "False" (5)
            _maxWidth = Math.Max(_maxWidth ?? 0, 5);
        }
    }

    /// <summary>
    /// Processes a string value using span-based processing for efficiency.
    /// </summary>
    /// <param name="value">The string value to process as a span</param>
    public void ProcessString(ReadOnlySpan<char> value)
    {
        lock (_lock)
        {
            // Skip empty strings
            if (value.IsWhiteSpace())
            {
                return;
            }

            // Check for mixing string with hard-typed values
            if (_isPrimedWithBonafideType)
            {
                throw new MixedTypingException(
                    "Cannot process string values after processing hard-typed objects. Guesser instances must be used with either strings OR hard-typed objects, not mixed with untyped objects.");
            }

            _valueCount++;

            // Calculate string length and check for unicode
            var length = CalculateStringLength(value);
            _maxWidth = Math.Max(_maxWidth ?? 0, length);

            // If already fallen back to string, just track width
            if (_currentType == typeof(string))
            {
                return;
            }

            // Loop until we find a compatible type, instead of recursion to avoid double-counting
            while (_currentType != typeof(string))
            {
                // Try to validate against current type
                var tempRequest = new DatabaseTypeRequest(_currentType)
                {
                    Width = _maxWidth,
                    Unicode = _requiresUnicode
                };
                tempRequest.Size.IncreaseTo(_digitsBeforeDecimal, _digitsAfterDecimal);

                var decider = _typeDeciders.Dictionary[_currentType];
                if (decider.IsAcceptableAsType(value, tempRequest))
                {
                    _validTypesSeen = decider.CompatibilityGroup;

                    // Update sizes from the decider's modifications
                    _digitsBeforeDecimal = tempRequest.Size.NumbersBeforeDecimalPlace;
                    _digitsAfterDecimal = tempRequest.Size.NumbersAfterDecimalPlace;

                    if (_currentType == typeof(DateTime))
                    {
                        _maxWidth = Math.Max(_maxWidth ?? 0, Guesser.MinimumLengthRequiredForDateStringRepresentation);
                    }
                    return;
                }

                // Not compatible - fall back to next type
                ChangeEstimateToNext();
            }
        }
    }

    /// <summary>
    /// Processes an arbitrary object value (null-safe).
    /// </summary>
    /// <param name="value">The value to process</param>
    public void Process(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            lock (_lock)
            {
                _nullCount++;
            }
            return;
        }

        // Fast path for common hard-typed values
        switch (value)
        {
            case int i:
                ProcessIntZeroAlloc(i);
                return;
            case decimal d:
                ProcessDecimalZeroAlloc(d);
                return;
            case bool b:
                ProcessBoolZeroAlloc(b);
                return;
            case string s:
                ProcessString(s.AsSpan());
                return;
        }

        // Generic fallback for other types
        lock (_lock)
        {
            // Check for mixed typing
            if (_validTypesSeen != TypeCompatibilityGroup.None || _currentType == typeof(string))
            {
                throw new MixedTypingException(ErrorFormatters.MixedTypingGenericTypeAfterString(value.GetType()));
            }

            _valueCount++;

            if (!_isPrimedWithBonafideType)
            {
                _currentType = value.GetType();
                _isPrimedWithBonafideType = true;
            }
            else if (_currentType != value.GetType())
            {
                throw new MixedTypingException(
                    $"We were adjusting to compensate for object '{value}' which is of Type '{value.GetType()}', we were previously passed a '{_currentType}' type");
            }

            // Use decider if available
            var valueString = value.ToString() ?? string.Empty;
            _maxWidth = Math.Max(_maxWidth ?? 0, CalculateStringLength(valueString.AsSpan()));

            if (_typeDeciders.Dictionary.TryGetValue(value.GetType(), out var decider))
            {
                var tempRequest = new DatabaseTypeRequest(_currentType);
                decider.IsAcceptableAsType(valueString.AsSpan(), tempRequest);
                _digitsBeforeDecimal = Math.Max(_digitsBeforeDecimal, tempRequest.Size.NumbersBeforeDecimalPlace);
                _digitsAfterDecimal = Math.Max(_digitsAfterDecimal, tempRequest.Size.NumbersAfterDecimalPlace);
            }
        }
    }

    private int CalculateStringLength(ReadOnlySpan<char> text)
    {
        // Fast path: no extra length and already seen unicode
        if (ExtraLengthPerNonAsciiCharacter == 0)
        {
            if (_requiresUnicode)
            {
                return text.Length;
            }

            // Check for unicode
            foreach (var c in text)
            {
                if (!char.IsAscii(c))
                {
                    _requiresUnicode = true;
                    return text.Length;
                }
            }
            return text.Length;
        }

        // Count non-ASCII characters
        var nonAsciiCount = 0;
        foreach (var c in text)
        {
            if (!char.IsAscii(c))
            {
                nonAsciiCount++;
            }
        }

        if (nonAsciiCount > 0)
        {
            _requiresUnicode = true;
        }

        return text.Length + (nonAsciiCount * ExtraLengthPerNonAsciiCharacter);
    }

    private void ChangeEstimateToNext()
    {
        var current = DatabaseTypeRequest.PreferenceOrder.IndexOf(_currentType);

        if (_validTypesSeen == TypeCompatibilityGroup.None)
        {
            _currentType = DatabaseTypeRequest.PreferenceOrder[current + 1];
        }
        else
        {
            var nextEstimate = DatabaseTypeRequest.PreferenceOrder[current + 1];

            if (nextEstimate == typeof(string) || _validTypesSeen == TypeCompatibilityGroup.Exclusive)
            {
                _currentType = typeof(string);
            }
            else if (_typeDeciders.Dictionary[nextEstimate].CompatibilityGroup == _validTypesSeen)
            {
                _currentType = nextEstimate;
            }
            else
            {
                _currentType = typeof(string);
            }
        }
    }

    private int CalculateDecimalStringLength()
    {
        if (_digitsBeforeDecimal == 0 && _digitsAfterDecimal == 0)
        {
            return 0;
        }

        var length = _digitsBeforeDecimal + _digitsAfterDecimal;
        if (_digitsAfterDecimal > 0)
        {
            length++; // decimal point
        }
        return length;
    }
}
