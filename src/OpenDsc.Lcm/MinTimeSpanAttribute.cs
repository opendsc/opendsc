// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel.DataAnnotations;

namespace OpenDsc.Lcm;

/// <summary>
/// Validation attribute that ensures a TimeSpan value is greater than or equal to a minimum value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinTimeSpanAttribute : ValidationAttribute
{
    private readonly TimeSpan _minValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinTimeSpanAttribute"/> class.
    /// </summary>
    /// <param name="minValue">The minimum allowed TimeSpan value in string format (e.g., "00:00:01").</param>
    public MinTimeSpanAttribute(string minValue)
    {
        _minValue = TimeSpan.Parse(minValue);
        ErrorMessage = $"Interval must be greater than {_minValue}";
    }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan <= _minValue)
            {
                return new ValidationResult(ErrorMessage ?? $"The field {validationContext.DisplayName} must be greater than {_minValue}.");
            }
        }

        return ValidationResult.Success;
    }
}
