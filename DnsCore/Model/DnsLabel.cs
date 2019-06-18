using System;
using System.Numerics;

using DnsCore.Encoding;

namespace DnsCore.Model;

public readonly struct DnsLabel : IEquatable<DnsLabel>, IEqualityOperators<DnsLabel, DnsLabel, bool>
{
    private const byte MaxLength = 63;
    private static readonly System.Text.Encoding Encoding = System.Text.Encoding.ASCII;

    public static DnsLabel Empty { get; } = new(ReadOnlyMemory<char>.Empty);

    private readonly ReadOnlyMemory<char> _label;

    public int Length => _label.Length;

    public bool IsEmpty => _label.IsEmpty;

    private DnsLabel(ReadOnlyMemory<char> label)
    {
        if (label.Length > MaxLength)
            throw new ArgumentException("Label length exceeds maximum length", nameof(label));

        if (label.Length > 0)
        {
            var span = label.Span;
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

        _label = label;
        return;

        static bool IsLetterOrDigit(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');
        static bool IsLetterOrDigitOrHyphen(char c) => IsLetterOrDigit(c) || c == '-';
    }

    internal static DnsLabel Parse(ReadOnlyMemory<char> label)
    {
        switch (label.Length)
        {
            case 0:
                return Empty;
            default:
                try
                {
                    return new DnsLabel(label);
                }
                catch (ArgumentException e)
                {
                    throw new FormatException(e.Message, e);
                }
        }
    }

    public static DnsLabel Parse(string label) => Parse(label.AsMemory());

    internal readonly void Encode(ref DnsWriter writer)
    {
        writer.Write((byte)Length);
        if (!IsEmpty)
            Encoding.GetBytes(_label.Span, writer.Advance(Length).Buffer);
    }

    internal static DnsLabel Decode(ref DnsReader reader)
    {
        var length = reader.Read<byte>();
        return length == 0
            ? Empty
            : new DnsLabel(Encoding.GetString(reader.Read(length)).AsMemory());
    }

    public override string ToString() => _label.ToString();

    public override bool Equals(object? obj) => obj is DnsLabel label && Equals(label);

    public bool Equals(DnsLabel other) => _label.Span.Equals(other._label.Span, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => String.GetHashCode(_label.Span, StringComparison.OrdinalIgnoreCase);

    public static bool operator ==(DnsLabel left, DnsLabel right) => left.Equals(right);

    public static bool operator !=(DnsLabel left, DnsLabel right) => !(left == right);
}