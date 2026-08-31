using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordMailInformationDataEncoder : DnsRecordDataEncoder<DnsMailInformationRecordData>
{
    public static readonly DnsRecordMailInformationDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsMailInformationRecordData data)
    {
        DnsNameEncoder.Encode(ref writer, data.ResponsibleMailbox);
        DnsNameEncoder.Encode(ref writer, data.ErrorMailbox);
    }

    protected override DnsMailInformationRecordData DecodeData(ref DnsReader reader)
    {
        var responsibleMailbox = DnsNameEncoder.Decode(ref reader);
        var errorMailbox = DnsNameEncoder.Decode(ref reader);
        return new(responsibleMailbox, errorMailbox);
    }

    protected override DnsRecord<DnsMailInformationRecordData> CreateRecord(DnsName name, DnsMailInformationRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsMailInformationRecord(name, data.ResponsibleMailbox, data.ErrorMailbox, ttl);
    }
}