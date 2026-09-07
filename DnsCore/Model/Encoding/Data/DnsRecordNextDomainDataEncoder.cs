using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordNextDomainDataEncoder : DnsRecordDataEncoder<DnsNextDomainRecordData>
{
    public static readonly DnsRecordNextDomainDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsNextDomainRecordData data)
    {
        DnsNameEncoder.Encode(ref writer, data.NextDomainName, false);
        writer.Write(data.TypeBitmap);
    }

    protected override DnsNextDomainRecordData DecodeData(ref DnsReader reader)
    {
        var nextDomainName = DnsNameEncoder.Decode(ref reader);
        return new(nextDomainName, [.. reader.ReadToEnd()]);
    }

    protected override DnsRecord<DnsNextDomainRecordData> CreateRecord(DnsName name, DnsNextDomainRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsNextDomainRecord(name, data, @class, ttl);
    }

    protected override void ValidateData(DnsNextDomainRecordData data)
    {
        DnsNextDomainRecordData.Validate(data);
        ValidateDataLength(checked(GetUncompressedLength(data.NextDomainName) + data.TypeBitmap.Length));
    }
}