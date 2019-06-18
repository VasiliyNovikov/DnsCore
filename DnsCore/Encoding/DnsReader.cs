using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using DnsCore.Model;

namespace DnsCore.Encoding;

internal ref struct DnsReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private Dictionary<int, DnsName>? _offsets;

    public int Position { get; private set; }

    public readonly DnsReader Seek(int position) => new(_buffer[position..]);
    public readonly DnsReader Seek(int position, int length) => new(_buffer.Slice(position, length));

    public readonly TInt Peek<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        return TInt.ReadBigEndian(_buffer.Slice(Position, Unsafe.SizeOf<TInt>()), TInt.IsZero(TInt.MinValue));
    }

    public TInt Read<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        var result = Peek<TInt>();
        Position += Unsafe.SizeOf<TInt>();
        return result;
    }

    public ReadOnlySpan<byte> Read(int length)
    {
        var newPosition = Position + length;
        if (newPosition > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        var result = _buffer[Position..newPosition];
        Position = newPosition;
        return result;
    }

    public ReadOnlySpan<byte> ReadToEnd()
    {
        var result = _buffer[Position..];
        Position = _buffer.Length;
        return result;
    }

    internal readonly bool GetNameByOffset(int offset, [MaybeNullWhen(false)] out DnsName name)
    {
        if (_offsets is not null && _offsets.TryGetValue(offset, out name))
            return true;
        name = null;
        return false;
    }

    internal void AddNameOffset(DnsName name, int offset) => (_offsets ??= new(1)).Add(offset, name);
}