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

    public void Write<TInt>(TInt value) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        var newPosition = GetNextPosition(value.GetByteCount());
        value.WriteBigEndian(_buffer[Position..newPosition]);
        Position = newPosition;
    }

    public void WriteTime(TimeSpan value) => Write(checked((uint)value.TotalSeconds));

    public void Write(ReadOnlySpan<byte> value)
    {
        var newPosition = GetNextPosition(value.Length);
        value.CopyTo(_buffer[Position..newPosition]);
        Position = newPosition;
    }

    public Span<byte> ProvideBufferAndAdvance(ushort length)
    {
        var oldPosition = Position;
        var newPosition = GetNextPosition(length);
        Position = newPosition;
        return _buffer[oldPosition..newPosition];
    }

    private readonly ushort GetNextPosition(int length)
    {
        if (length > UInt16.MaxValue - Position)
            throw new ArgumentOutOfRangeException(nameof(length), $"Message exceeds the maximum length of {UInt16.MaxValue} bytes");
        var newPosition = (ushort)(Position + length);
        if (newPosition > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length), $"Buffer size {_buffer.Length} is too short");
        return newPosition;

    }

    internal readonly bool GetNameOffset(DnsName name, out int offset) => _offsets.TryGetValue(name, out offset);

    internal readonly void AddNameOffset(DnsName name, int offset) => _offsets.Add(name, offset);
}