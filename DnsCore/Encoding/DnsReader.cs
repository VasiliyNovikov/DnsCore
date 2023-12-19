using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using DnsCore.Model;

namespace DnsCore.Encoding;

internal ref struct DnsReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private readonly int _length;
    private readonly Dictionary<int, DnsName> _offsets = new(1);

    public int Position { get; private set; }

    private DnsReader(ReadOnlySpan<byte> buffer, int position, int length)
    {
        if (position < 0 || position > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        var actualLength = position + length;
        if (actualLength < 0 || actualLength > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = buffer;
        Position = position;
        _length = actualLength;
    }

    public DnsReader(ReadOnlySpan<byte> buffer) : this(buffer, 0, buffer.Length) { }

    public readonly DnsReader Seek(int position) => new(_buffer, position, _buffer.Length - position);
    public readonly DnsReader Seek(int position, int length) => new(_buffer, position, length);

    private readonly ReadOnlySpan<byte> Peek(int length)
    {
        var newPosition = Position + length;
        return newPosition > _length
            ? throw new ArgumentOutOfRangeException(nameof(length))
            : _buffer[Position..newPosition];
    }

    public readonly TInt Peek<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        return TInt.ReadBigEndian(Peek(Unsafe.SizeOf<TInt>()), TInt.IsZero(TInt.MinValue));
    }

    public TInt Read<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        var result = Peek<TInt>();
        Position += Unsafe.SizeOf<TInt>();
        return result;
    }

    public ReadOnlySpan<byte> Read(int length)
    {
        var result = Peek(length);
        Position += length;
        return result;
    }

    public ReadOnlySpan<byte> ReadToEnd() => Read(_length - Position);

    internal readonly bool GetNameByOffset(int offset, [MaybeNullWhen(false)] out DnsName name) => _offsets.TryGetValue(offset, out name);

    internal readonly void AddNameOffset(DnsName name, int offset) => _offsets.Add(offset, name);
}