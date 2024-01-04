using System;
using System.Numerics;

using DnsCore.Encoding;

using Microsoft.Extensions.Primitives;

namespace DnsCore.Model;

public sealed class DnsName
    : IEquatable<DnsName>
    , IEqualityOperators<DnsName, DnsName, bool>
    , ISpanFormattable
{
    private const byte MaxLength = 255;
    private const byte CompressionMask = 0b1100_0000;
    private const ushort OffsetMask = CompressionMask << 8;
    private const ushort OffsetMaskInverted = unchecked((ushort)~OffsetMask);
    private const char Separator = '.';

    public static DnsName Empty { get; } = new(DnsLabel.Empty, null);

    public int Length { get; }

    public bool IsEmpty => Length == 1;

    public DnsLabel Label { get; }

    public DnsName? Parent { get; }

    public DnsName(DnsLabel label, DnsName? parent)
    {
        if (label.IsEmpty && parent is not null)
            throw new ArgumentException("Parent name should be null if name label is empty", nameof(parent));

        var length = label.Length + 1;
        if (parent is not null && !parent.IsEmpty)
        {
            length += parent.Length;
            if (length > MaxLength)
                throw new ArgumentException("Name length exceeds maximum length", nameof(parent));
        }

        Label = label;
        Parent = parent;
        Length = length;
    }

    private static DnsName ParseCore(StringSegment name)
    {
        if (name.Length == 0)
            return Empty;

        var lastIndex = name.Length - 1;
        if (name[lastIndex] == Separator)
            name = name.Subsegment(0, lastIndex);

        if (name.Length == 0)
            return Empty;

        var separatorIndex = name.IndexOf(Separator);
        try
        {
            return separatorIndex == -1
                ? new DnsName(DnsLabel.ParseCore(name), Empty)
                : new DnsName(DnsLabel.ParseCore(name.Subsegment(0, separatorIndex)), ParseCore(name.Subsegment(separatorIndex + 1)));
        }
        catch (ArgumentException e)
        {
            throw new FormatException(e.Message, e);
        }
    }

    public static DnsName Parse(string name) => ParseCore(name);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!Label.TryFormat(destination, out charsWritten, format, provider))
            return false;

        if (charsWritten >= destination.Length)
            return false;

        destination[charsWritten++] = Separator;

        if (Parent is null || Parent.IsEmpty)
            return true;

        if (Parent.TryFormat(destination[charsWritten..], out var parentCharsWritten, format, provider))
        {
            charsWritten += parentCharsWritten;
            return true;
        }

        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> buffer = stackalloc char[Length];
        TryFormat(buffer, out _, default, formatProvider);
        return new string(buffer);
    }

    public override string ToString() => ToString(default, default);

    internal void Encode(ref DnsWriter writer)
    {
        if (!IsEmpty)
        {
            if (writer.GetNameOffset(this, out var offset))
            {
                writer.Write((ushort)(offset | OffsetMask));
                return;
            }
            writer.AddNameOffset(this, writer.Position);
        }

        Label.Encode(ref writer);
        Parent?.Encode(ref writer);
    }

    internal static DnsName Decode(ref DnsReader reader)
    {
        if (reader.Peek<byte>() >= CompressionMask)
        {
            var offset = reader.Read<ushort>() & OffsetMaskInverted;
            if (!reader.GetNameByOffset(offset, out var name))
            {
                var offsetReader = reader.GetSubReader(offset);
                name = Decode(ref offsetReader);
                reader.AddNameOffset(name, offset);
            }
            return name;
        }
        else
        {
            var offset = reader.Position;
            var label = DnsLabel.Decode(ref reader);
            if (label.IsEmpty)
                return Empty;

            var parent = Decode(ref reader);
            var name = new DnsName(label, parent);
            reader.AddNameOffset(name, offset);
            return name;
        }
    }

    public bool Equals(DnsName? other)
    {
        return other is not null &&
               Length == other.Length &&
               Label == other.Label &&
               (Parent?.Equals(other.Parent) ?? other.Parent is null);
    }

    public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

    public override int GetHashCode() => HashCode.Combine(Label, Parent);

    public static bool operator ==(DnsName? left, DnsName? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(DnsName? left, DnsName? right) => !(left == right);

    public static explicit operator DnsName(string name) => Parse(name);

    public static explicit operator string(DnsName name) => name.ToString();
}