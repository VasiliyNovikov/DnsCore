using System;
using System.Numerics;

using DnsCore.Encoding;

using Microsoft.Extensions.Primitives;

namespace DnsCore.Model;

public readonly struct DnsLabel
    : IEquatable<DnsLabel>
    , IEqualityOperators<DnsLabel, DnsLabel, bool>
    , ISpanFormattable
{
    private const byte MaxLength = 63;
    private static readonly System.Text.Encoding Encoding = System.Text.Encoding.ASCII;

    public static DnsLabel Empty { get; } = new(StringSegment.Empty);

    private readonly StringSegment _label;

    public int Length => _label.Length;

    public bool IsEmpty => _label.Length == 0;

    private DnsLabel(StringSegment label) => _label = label;

    private static void Validate(StringSegment label)
    {
        if (label.Length > MaxLength)
            throw new ArgumentException("Label length exceeds maximum length", nameof(label));

        if (label.Length > 0)
        {
            var span = label.AsSpan();
            if (!IsLetterOrDigit(span[0]))
                throw new ArgumentException("First character of the label must be an ASCII letter", nameof(label));

            if (label.Length > 1)
            {
                for (var i = 1; i < label.Length - 1; ++i)
                    if (!IsLetterOrDigitOrHyphen(span[i]))
                        throw new ArgumentException("Middle characters of the label must be an ASCII letter or digit or hyphen", nameof(label));

                if (!IsLetterOrDigit(span[^1]))
                    throw new ArgumentException("Last character of the label must be an ASCII letter or digit", nameof(label));
            }
        }

        static bool IsLetterOrDigit(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');
        static bool IsLetterOrDigitOrHyphen(char c) => IsLetterOrDigit(c) || c == '-';
    }   

    internal static DnsLabel ParseCore(StringSegment label)
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

    public static DnsLabel Parse(string label) => ParseCore(label);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_label.AsSpan().TryCopyTo(destination))
        {
            charsWritten = _label.Length;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> buffer = stackalloc char[Length];
        TryFormat(buffer, out _, default, formatProvider);
        return new string(buffer);
    }

    public override string ToString() => ToString(default, default);

    internal readonly void Encode(ref DnsWriter writer)
    {
        writer.Write((byte)Length);
        if (!IsEmpty)
            Encoding.GetBytes(_label.AsSpan(), writer.Advance(Length));
    }

    internal static DnsLabel Decode(ref DnsReader reader)
    {
        var length = reader.Read<byte>();
        if (length == 0)
            return Empty;
        else
        {
            var labelStr = Encoding.GetString(reader.Read(length));
            Validate(labelStr);
            return new DnsLabel(labelStr);
        }
    }

    public override bool Equals(object? obj) => obj is DnsLabel label && Equals(label);

    public bool Equals(DnsLabel other) => _label.Equals(other._label, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => _label.GetHashCode();

    public static bool operator ==(DnsLabel left, DnsLabel right) => left.Equals(right);

    public static bool operator !=(DnsLabel left, DnsLabel right) => !(left == right);

    public static explicit operator DnsLabel(string name) => Parse(name);

    public static explicit operator string(DnsLabel name) => name.ToString();
}