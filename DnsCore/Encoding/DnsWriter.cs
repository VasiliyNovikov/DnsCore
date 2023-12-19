using System;
using System.Collections.Generic;
using System.Numerics;

using DnsCore.Model;

namespace DnsCore.Encoding;

internal ref struct DnsWriter(Span<byte> buffer)
{
    private readonly Dictionary<DnsName, int> _offsets = new(1);

    public Span<byte> Buffer { get; } = buffer;
    public int Position { get; private set; }

    public void Write<TInt>(TInt value) where TInt : unmanaged, IBinaryInteger<TInt> => Position += value.WriteBigEndian(Buffer[Position..]);

    public void Write(ReadOnlySpan<byte> value)
    {
        value.CopyTo(Buffer[Position..]);
        Position += value.Length;
    }

    public Span<byte> Advance(int length)
    {
        var oldPosition = Position;
        var newPosition = oldPosition + length;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, Buffer.Length, nameof(length));

        Position = newPosition;
        return Buffer[oldPosition..newPosition];
    }

    internal readonly bool GetNameOffset(DnsName name, out int offset) => _offsets.TryGetValue(name, out offset);

    internal readonly void AddNameOffset(DnsName name, int offset) => _offsets.Add(name, offset);
}