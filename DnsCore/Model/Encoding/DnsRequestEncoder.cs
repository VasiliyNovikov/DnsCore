using System;

namespace DnsCore.Model.Encoding;

public static class DnsRequestEncoder
{
    public static ushort Encode(Span<byte> buffer, DnsRequest request) => DnsMessageEncoder.Encode(buffer, request);
    public static DnsRequest Decode(ReadOnlySpan<byte> buffer) => new(DnsRawMessageEncoder.Decode(buffer));
}