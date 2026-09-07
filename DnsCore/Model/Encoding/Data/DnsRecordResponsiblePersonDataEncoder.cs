using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordResponsiblePersonDataEncoder : DnsRecordDataEncoder<DnsResponsiblePersonRecordData>
{
    public static readonly DnsRecordResponsiblePersonDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsResponsiblePersonRecordData data)
    {
        DnsNameEncoder.Encode(ref writer, data.Mailbox, false);
        DnsNameEncoder.Encode(ref writer, data.TextName, false);
    }

    protected override DnsResponsiblePersonRecordData DecodeData(ref DnsReader reader)
    {
        return new(DnsNameEncoder.Decode(ref reader), DnsNameEncoder.Decode(ref reader));
    }

    protected override DnsRecord<DnsResponsiblePersonRecordData> CreateRecord(DnsName name, DnsResponsiblePersonRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsResponsiblePersonRecord(name, data.Mailbox, data.TextName, @class, ttl);
    }

    protected override void ValidateData(DnsResponsiblePersonRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.Mailbox);
        ArgumentNullException.ThrowIfNull(data.TextName);
        ValidateDataLength(checked(GetUncompressedLength(data.Mailbox) + GetUncompressedLength(data.TextName)));
    }
}