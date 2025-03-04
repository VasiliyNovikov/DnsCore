using System;

namespace DnsCore.Model.Encoding;

public static class DnsMessageEncoder
{
    public static int Encode(Span<byte> buffer, DnsMessage message) => DnsRawMessageEncoder.Encode(buffer, message.ToRawMessage());
}