using System;

namespace DnsCore.Model.Encoding;

public static class DnsResponseEncoder
{
    public static ushort Encode(Span<byte> buffer, DnsResponse response) => DnsMessageEncoder.Encode(buffer, response);
    public static DnsResponse Decode(ReadOnlySpan<byte> buffer) => new(DnsRawMessageEncoder.Decode(buffer));
}