using System;
using System.Runtime.CompilerServices;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordTextDataEncoder : DnsRecordDataEncoder<string>
{
    private const ushort MaxSegmentLength = 255;

    public static readonly DnsRecordTextDataEncoder Instance = new();

    [SkipLocalsInit]
    protected override void EncodeData(ref DnsWriter writer, string data)
    {
        Span<byte> buffer = stackalloc byte[DnsTextRecord.Encoding.GetMaxByteCount(data.Length)];
        buffer = buffer[..DnsTextRecord.Encoding.GetBytes(data, buffer)];
        while (!buffer.IsEmpty)
        {
            var segmentLength = (byte)Math.Min(buffer.Length, MaxSegmentLength);
            writer.Write(segmentLength);
            buffer[..segmentLength].CopyTo(writer.ProvideBufferAndAdvance(segmentLength));
            buffer = buffer[segmentLength..];
        }
    }

    [SkipLocalsInit]
    protected override string DecodeData(ref DnsReader reader)
    {
        var encodedBuffer = reader.ReadToEnd();
        Span<byte> buffer = stackalloc byte[encodedBuffer.Length];
        var bufferSlice = buffer;
        while (!encodedBuffer.IsEmpty)
        {
            var segmentLength = encodedBuffer[0];
            encodedBuffer = encodedBuffer[1..];
            encodedBuffer[..segmentLength].CopyTo(bufferSlice);
            bufferSlice = bufferSlice[segmentLength..];
        }
        return DnsTextRecord.Encoding.GetString(buffer[..^bufferSlice.Length]);
    }

    protected override DnsRecord<string> CreateRecord(DnsName name, string data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl) => new DnsTextRecord(name, data, ttl);
}