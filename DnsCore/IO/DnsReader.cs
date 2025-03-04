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
    private readonly Dictionary<int, DnsName> _offsets = new(1);

    public int Position { get; private set; }

    private DnsReader(ReadOnlySpan<byte> originalBuffer, int position, int length)
    {
        _originalBuffer = originalBuffer;
        _slicedBuffer = originalBuffer[..(position + length)];
        Position = position;
    }

    public DnsReader(ReadOnlySpan<byte> buffer) : this(buffer, 0, buffer.Length) { }

    public readonly DnsReader GetSubReader(int position) => new(_originalBuffer, position, _originalBuffer.Length - position);
    public readonly DnsReader GetSubReader(int position, int length) => new(_originalBuffer, position, length);

    private readonly ReadOnlySpan<byte> Peek(int length) => _slicedBuffer.Slice(Position, length);

    public readonly TInt Peek<TInt>() where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        return TInt.ReadBigEndian(Peek(Unsafe.SizeOf<TInt>()), isUnsigned: TInt.IsZero(TInt.MinValue));
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
    
    public void Skip(int length) => Read(length);

    public ReadOnlySpan<byte> ReadToEnd() => Read(_slicedBuffer.Length - Position);

    internal readonly bool GetNameByOffset(int offset, [MaybeNullWhen(false)] out DnsName name) => _offsets.TryGetValue(offset, out name);

    internal readonly void AddNameOffset(DnsName name, int offset) => _offsets.Add(offset, name);
}