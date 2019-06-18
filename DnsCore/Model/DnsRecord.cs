using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public abstract class DnsRecord(DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsQuestion(name, recordType, @class)
{
    public TimeSpan Ttl { get; } = ttl;

    public override string ToString() => $"{base.ToString()} {(uint)Ttl.TotalSeconds,-5}";

    internal override void Encode(ref DnsWriter writer)
    {
        base.Encode(ref writer);
        writer.Write((uint)Ttl.TotalSeconds);

        var dataLenWriter = writer.Advance(2);

        var dataLenPosition = writer.Position;
        EncodeData(ref writer);

        dataLenWriter.Write((ushort)(writer.Position - dataLenPosition));
    }

    private protected abstract void EncodeData(ref DnsWriter writer);

    internal static new DnsRecord Decode(ref DnsReader reader)
    {
        var questionPart = DnsQuestion.Decode(ref reader);
        var ttl = TimeSpan.FromSeconds(reader.Read<uint>());
        var dataLength = reader.Read<ushort>();
        var dataReader = reader.Seek(reader.Position, dataLength);
        reader.Read(dataLength);

        switch (questionPart.RecordType)
        {
            case DnsRecordType.A or DnsRecordType.AAAA:
                var address = DnsAddressRecord.DecodeData(ref dataReader);
                return new DnsAddressRecord(questionPart.Name, address, ttl);
            case DnsRecordType.CNAME:
                var alias = DnsCNameRecord.DecodeData(ref dataReader);
                return new DnsCNameRecord(questionPart.Name, alias, ttl);
            case DnsRecordType.PTR:
                var ptrName = DnsPtrRecord.DecodeData(ref dataReader);
                return new DnsPtrRecord(questionPart.Name, ptrName, ttl);
            default:
                var data = DnsRawRecord.DecodeData(ref dataReader);
                return new DnsRawRecord(questionPart.Name, questionPart.RecordType, questionPart.Class, ttl, data);
        }
    }
}