using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordServiceDataEncoder : DnsRecordDataEncoder<DnsServiceRecordData>
{
    public static readonly DnsRecordServiceDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsServiceRecordData data)
    {
        writer.Write(data.Priority);
        writer.Write(data.Weight);
        writer.Write(data.Port);
        DnsNameEncoder.Encode(ref writer, data.Target, false);
    }

    protected override DnsServiceRecordData DecodeData(ref DnsReader reader)
    {
        var priority = reader.Read<ushort>();
        var weight = reader.Read<ushort>();
        var port = reader.Read<ushort>();
        var target = DnsNameEncoder.Decode(ref reader);
        return reader.ReadToEnd().IsEmpty
            ? new(priority, weight, port, target)
            : throw new FormatException("Invalid SRV record data: buffer contains extra data");
    }

    protected override DnsRecord<DnsServiceRecordData> CreateRecord(DnsName name, DnsServiceRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsServiceRecord(name, data.Priority, data.Weight, data.Port, data.Target, ttl);
    }
}