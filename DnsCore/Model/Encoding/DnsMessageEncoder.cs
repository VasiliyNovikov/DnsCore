using System;

namespace DnsCore.Model.Encoding;

public static class DnsMessageEncoder
{
    public static ushort Encode(Span<byte> buffer, DnsMessage message) => DnsRawMessageEncoder.Encode(buffer, message.ToRawMessage());
}