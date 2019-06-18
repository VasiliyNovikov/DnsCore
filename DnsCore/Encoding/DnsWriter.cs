using System;
using System.Collections.Generic;
using System.Numerics;

using DnsCore.Model;

namespace DnsCore.Encoding;

internal ref struct DnsWriter(Span<byte> buffer)
{
    private Dictionary<DnsName, int>? _offsets;

    public Span<byte> Buffer { get; } = buffer;
    public ushort Position { get; private set; }

    public void Write<TInt>(TInt value) where TInt : unmanaged, IBinaryInteger<TInt> => Position += (ushort)value.WriteBigEndian(Buffer[Position..]);

    public void Write(ReadOnlySpan<byte> value)
    {
        value.CopyTo(Buffer[Position..]);
        Position += (ushort)value.Length;
    }

    public DnsWriter Advance(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Buffer.Length - Position);
        var oldPosition = Position;
        Position = (ushort)(oldPosition + length);
        return new DnsWriter(Buffer[oldPosition..Position]);
    }

    internal bool GetNameOffset(DnsName name, out int offset)
    {
        if (_offsets is not null && _offsets.TryGetValue(name, out offset))
            return true;
        offset = 0;
        return false;
    }

    internal void AddNameOffset(DnsName name, int offset) => (_offsets ??= new Dictionary<DnsName, int>()).Add(name, offset);
}