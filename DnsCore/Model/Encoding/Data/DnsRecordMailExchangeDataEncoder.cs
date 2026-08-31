using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailExchangeDataEncoder : DnsRecordDataEncoder<DnsMailExchangeRecordData>
{
    public static readonly DnsRecordMailExchangeDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsMailExchangeRecordData data)
    {
        writer.Write(data.Preference);
        DnsNameEncoder.Encode(ref writer, data.Exchange);
    }

    protected override DnsMailExchangeRecordData DecodeData(ref DnsReader reader)
    {
        var preference = reader.Read<ushort>();
        var exchange = DnsNameEncoder.Decode(ref reader);
        return new(preference, exchange);
    }

    protected override DnsRecord<DnsMailExchangeRecordData> CreateRecord(DnsName name, DnsMailExchangeRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsMailExchangeRecord(name, data.Preference, data.Exchange, ttl);
    }
}