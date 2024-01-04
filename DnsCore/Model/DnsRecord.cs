using System;
using System.Buffers.Binary;

using DnsCore.Encoding;

namespace DnsCore.Model;

public abstract class DnsRecord(DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecordBase(name, recordType, @class)
{
    public TimeSpan Ttl => ttl;

    public override string ToString() => $"{base.ToString()} {(uint)ttl.TotalSeconds,-5}";

    internal override void Encode(ref DnsWriter writer)
    {
        base.Encode(ref writer);
        writer.Write((uint)ttl.TotalSeconds);

        var dataLenBuffer = writer.ProvideBufferAndAdvance(2);

        var dataPosition = writer.Position;
        EncodeData(ref writer);

        BinaryPrimitives.WriteUInt16BigEndian(dataLenBuffer, (ushort)(writer.Position - dataPosition));
    }

    private protected abstract void EncodeData(ref DnsWriter writer);

    internal new static DnsRecord Decode(ref DnsReader reader)
    {
        var (name, type, @class) = DnsRecordBase.Decode(ref reader);
        var ttl = TimeSpan.FromSeconds(reader.Read<uint>());
        var dataLength = reader.Read<ushort>();
        var dataReader = reader.GetSubReader(reader.Position, dataLength);
        reader.Skip(dataLength);

        switch (type)
        {
            case DnsRecordType.A or DnsRecordType.AAAA:
            {
                var data = DnsAddressRecord.DecodeData(ref dataReader);
                return new DnsAddressRecord(name, data, ttl);
            }
            case DnsRecordType.CNAME:
            {
                var data = DnsCNameRecord.DecodeData(ref dataReader);
                return new DnsCNameRecord(name, data, ttl);
            }
            case DnsRecordType.PTR:
            {
                var data = DnsPtrRecord.DecodeData(ref dataReader);
                return new DnsPtrRecord(name, data, ttl);
            }
            default:
            {
                var data = DnsRawRecord.DecodeData(ref dataReader);
                return new DnsRawRecord(name, data, type, @class, ttl);
            }
        }
    }
}

public abstract class DnsRecord<T>(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord(name, recordType, @class, ttl) where T : notnull
{
    public T Data => data;
}