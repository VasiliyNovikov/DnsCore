using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using DnsCore.Model;

namespace DnsCore.IO;

internal ref struct DnsReader
{
    private readonly ReadOnlySpan<byte> _originalBuffer;
    private readonly ReadOnlySpan<byte> _slicedBuffer;
    private readonly Dictionary<ushort, DnsName> _offsets = new(1);

    public ushort Position { get; private set; }

    private DnsReader(ReadOnlySpan<byte> originalBuffer, ushort position, ushort length)
    {
        _originalBuffer = originalBuffer;
        _slicedBuffer = originalBuffer[..(position + length)];
        Position = position;
    }

    public DnsReader(ReadOnlySpan<byte> buffer) : this(buffer, 0, (ushort)buffer.Length) { }

    public readonly DnsReader GetSubReader(ushort position)
    {
        if (position > _originalBuffer.Length)
            throw new FormatException("Invalid DNS message: position is out of buffer bounds");
        return new(_originalBuffer, position, checked((ushort)(_originalBuffer.Length - position)));
    }

    public readonly DnsReader GetSubReader(ushort position, ushort length)
    {
        if (position > _originalBuffer.Length)
            throw new FormatException("Invalid DNS message: sub-reader position is out of buffer bounds");
        if (length > _originalBuffer.Length - position)
            throw new FormatException("Invalid DNS message: sub-reader bounds exceed buffer size");
        return new(_originalBuffer, position, length);
    }

    private readonly ReadOnlySpan<byte> Peek(ushort length) => _slicedBuffer.Slice(Position, length);

    public readonly TInt Peek<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        return TInt.ReadBigEndian(Peek((ushort)Unsafe.SizeOf<TInt>()), isUnsigned: TInt.IsZero(TInt.MinValue));
    }

    public TInt Read<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        var result = Peek<TInt>();
        Position += (ushort)Unsafe.SizeOf<TInt>();
        return result;
    }

    public ReadOnlySpan<byte> Read(ushort length)
    {
        var result = Peek(length);
        Position += length;
        return result;
    }

    public void Skip(ushort length) => Read(length);

    public ReadOnlySpan<byte> ReadToEnd() => Read((ushort)(_slicedBuffer.Length - Position));

    internal readonly bool GetNameByOffset(ushort offset, [MaybeNullWhen(false)] out DnsName name) => _offsets.TryGetValue(offset, out name);

    internal readonly void AddNameOffset(ushort offset, DnsName name) => _offsets.Add(offset, name);
}