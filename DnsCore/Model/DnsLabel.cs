using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DnsCore.Model;

public readonly struct DnsLabel
    : IEquatable<DnsLabel>
    , IEqualityOperators<DnsLabel, DnsLabel, bool>
    , ISpanFormattable
{
    private const byte MaxLength = 63;

    public static DnsLabel Empty { get; } = new(ReadOnlyMemory<char>.Empty);

    private readonly ReadOnlyMemory<char> _label;

    public ReadOnlySpan<char> Span => _label.Span;

    public byte Length => (byte)_label.Length;

    public bool IsEmpty => _label.Length == 0;

    public bool IsHostName
    {
        get
        {
            var text = Span;
            if (text.IsEmpty || !IsAlphaNumeric(text[0]) || (text.Length > 1 && !IsAlphaNumeric(text[^1])))
                return false;
            for (var i = 1; i < text.Length - 1; ++i)
                if (!IsAlphaNumeric(text[i]) && text[i] != '-')
                    return false;
            return true;

            static bool IsAlphaNumeric(char value) => value is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');
        }
    }

    internal DnsLabel(ReadOnlyMemory<char> label) => _label = label;

    internal static void Validate(ReadOnlyMemory<char> label)
    {
        if (GetValidationError(label) is { } error)
            throw new ArgumentException(error, nameof(label));
    }

    private static string? GetValidationError(ReadOnlyMemory<char> label)
    {
        if (label.Length > MaxLength)
            return "Label length exceeds maximum length";

        foreach (var value in label.Span)
            if (value is < '!' or > '~' or '.' or '\\')
                return "DNS labels require printable ASCII without spaces, dots, or backslashes";

        return null;
    }

    internal static bool TryParseCore(ReadOnlyMemory<char> label, out DnsLabel result, [NotNullWhen(false)] out string? validationError)
    {
        if ((validationError = GetValidationError(label)) is not null)
        {
            result = default;
            return false;
        }

        result = label.IsEmpty ? Empty : new DnsLabel(label);
        return true;
    }

    /// <summary>Parses printable ASCII label text without spaces, dots, or backslashes.</summary>
    public static DnsLabel Parse(string? label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return TryParseCore(label.AsMemory(), out var result, out var error) ? result : throw new FormatException(error);
    }

    /// <summary>Tries to parse printable ASCII label text. Null or invalid input returns false and an empty label.</summary>
    public static bool TryParse(string? label, out DnsLabel result)
    {
        result = default;
        return label is not null && TryParseCore(label.AsMemory(), out result, out _);
    }

    public static DnsLabel ParseHostName(string? label)
    {
        return Parse(label) is { IsHostName: true } result
            ? result
            : throw new FormatException("Invalid hostname label");
    }

    public static bool TryParseHostName(string? label, out DnsLabel result) => TryParse(label, out result) && result.IsHostName;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (Span.TryCopyTo(destination))
        {
            charsWritten = _label.Length;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    [SkipLocalsInit]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> buffer = stackalloc char[Length];
        TryFormat(buffer, out _, default, formatProvider);
        return new string(buffer);
    }

    public override string ToString() => ToString(null, null);

    public override bool Equals(object? obj) => obj is DnsLabel label && Equals(label);

    public bool Equals(DnsLabel other) => Span.Equals(other.Span, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => string.GetHashCode(Span, StringComparison.OrdinalIgnoreCase);

    public static bool operator ==(DnsLabel left, DnsLabel right) => left.Equals(right);

    public static bool operator !=(DnsLabel left, DnsLabel right) => !(left == right);

    public static explicit operator DnsLabel(string name) => Parse(name);

    public static explicit operator string(DnsLabel name) => name.ToString();
}