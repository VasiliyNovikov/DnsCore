using System;
using System.Runtime.CompilerServices;

namespace DnsCore.Utils;

internal static class ArgumentOutOfRangeExceptionExtensions
{
    extension(ArgumentOutOfRangeException)
    {
        public static void ThrowIfNegativeOrZero(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} ('{value}') must be a non-negative and non-zero value.");
        }

        public static void ThrowIfNegative(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} ('{value}') must be a non-negative value.");
        }
    }
}