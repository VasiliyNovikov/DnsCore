using System;
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
        if (label.Length > MaxLength)
            throw new ArgumentException("Label length exceeds maximum length", nameof(label));

        foreach (var value in label.Span)
            if (value is < '!' or > '~' or '.' or '\\')
                throw new ArgumentException("DNS labels require printable ASCII without spaces, dots, or backslashes", nameof(label));
    }

    internal static DnsLabel ParseCore(ReadOnlyMemory<char> label)
    {
        switch (label.Length)
        {
            case 0:
                return Empty;
            default:
                try
                {
                    Validate(label);
                    return new DnsLabel(label);
                }
                catch (ArgumentException e)
                {
                    throw new FormatException(e.Message, e);
                }
        }
    }

    /// <summary>Parses printable ASCII label text without spaces, dots, or backslashes.</summary>
    public static DnsLabel Parse(string label) => ParseCore(label.AsMemory());

    public static DnsLabel ParseHostName(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        var result = Parse(label);
        return result.IsHostName ? result : throw new FormatException("Invalid hostname label");
    }

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