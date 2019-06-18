using System;
using System.Numerics;
using System.Text;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsName : IEquatable<DnsName>, IEqualityOperators<DnsName, DnsName, bool>
{
    private const byte MaxLength = 255;
    private const byte CompressionMask = 0b1100_0000;
    private const ushort OffsetMask = CompressionMask << 8;
    private const ushort OffsetMaskInverted = unchecked((ushort)~OffsetMask);
    private const char Separator = '.';

    private readonly DnsLabel _label;
    private readonly DnsName? _parent;

    public static DnsName Empty { get; } = new(DnsLabel.Empty, null);

    public int Length { get; }

    public bool IsEmpty => Length == 1;

    private DnsName(DnsLabel label, DnsName? parent)
    {
        if (label.IsEmpty && parent is not null)
            throw new ArgumentException("Parent name should be null if name label is empty", nameof(parent));

        var length = label.Length + 1 + (parent?.Length ?? 0);
        if (length > MaxLength)
            throw new ArgumentException("Name length exceeds maximum length", nameof(label));

        _label = label;
        _parent = parent;
        Length = length;
    }

    private static DnsName Parse(ReadOnlyMemory<char> name)
    {
        if (name.IsEmpty)
            return Empty;

        if (name.Span[^1] == Separator)
            name = name[..^1];

        if (name.IsEmpty)
            return Empty;

        var separatorIndex = name.Span.IndexOf(Separator);
        try
        {
            return separatorIndex == -1
                ? new DnsName(DnsLabel.Parse(name), Empty)
                : new DnsName(DnsLabel.Parse(name[..separatorIndex]), Parse(name[(separatorIndex + 1)..]));
        }
        catch (ArgumentException e)
        {
            throw new FormatException(e.Message, e);
        }
    }

    public static DnsName Parse(string name) => Parse(name.AsMemory());

    public override string ToString()
    {
        var builder = new StringBuilder(Length);
        builder.Append(_label.ToString());
        builder.Append('.');
        if (_parent is not null && !_parent.IsEmpty)
            builder.Append(_parent);
        return builder.ToString();
    }

    internal void Encode(ref DnsWriter writer)
    {
        if (Length > 0 && writer.GetNameOffset(this, out var offset))
        {
            writer.Write((ushort)(offset | 0b1100_0000_0000_0000));
            return;
        }

        writer.AddNameOffset(this, writer.Position);

        _label.Encode(ref writer);
        _parent?.Encode(ref writer);
    }

    internal static DnsName Decode(ref DnsReader reader)
    {
        if (reader.Peek<byte>() > CompressionMask)
        {
            var offset = reader.Read<ushort>() & OffsetMaskInverted;
            if (!reader.GetNameByOffset(offset, out var name))
            {
                var offsetReader = reader.Seek(offset);
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
               _label == other._label &&
               (_parent is null
                   ? other._parent is null
                   : other._parent is not null && _parent.Equals(other._parent));
    }

    public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

    public override int GetHashCode() => HashCode.Combine(_label, _parent);

    public static bool operator ==(DnsName? left, DnsName? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(DnsName? left, DnsName? right) => !(left == right);
}