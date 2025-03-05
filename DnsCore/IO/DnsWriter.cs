using System;
using System.Collections.Generic;
using System.Numerics;

using DnsCore.Model;

namespace DnsCore.IO;

internal ref struct DnsWriter(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    private readonly Dictionary<DnsName, int> _offsets = new(1);

    public ushort Position { get; private set; }

    public void Write<TInt>(TInt value) where TInt : unmanaged, IBinaryInteger<TInt> => Position += (ushort)value.WriteBigEndian(_buffer[Position..]);

    public void Write(ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer[Position..]);
        Position += (ushort)value.Length;
    }

    public Span<byte> ProvideBufferAndAdvance(ushort length)
    {
        var oldPosition = Position;
        var newPosition = oldPosition + length;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, _buffer.Length, nameof(length));

        Position = (ushort)newPosition;
        return _buffer[oldPosition..newPosition];
    }

    internal readonly bool GetNameOffset(DnsName name, out int offset) => _offsets.TryGetValue(name, out offset);

    internal readonly void AddNameOffset(DnsName name, int offset) => _offsets.Add(name, offset);
}