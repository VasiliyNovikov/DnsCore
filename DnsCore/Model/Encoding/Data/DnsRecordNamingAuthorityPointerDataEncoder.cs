using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordNamingAuthorityPointerDataEncoder : DnsRecordDataEncoder<DnsNamingAuthorityPointerRecordData>
{
    public static readonly DnsRecordNamingAuthorityPointerDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsNamingAuthorityPointerRecordData data)
    {
        writer.Write(data.Order);
        writer.Write(data.Preference);
        EncodeString(ref writer, data.Flags);
        EncodeString(ref writer, data.Services);
        EncodeString(ref writer, data.RegularExpression);
        DnsNameEncoder.Encode(ref writer, data.Replacement, false);
    }

    protected override DnsNamingAuthorityPointerRecordData DecodeData(ref DnsReader reader)
    {
        var order = reader.Read<ushort>();
        var preference = reader.Read<ushort>();
        var flags = DecodeString(ref reader);
        var services = DecodeString(ref reader);
        var regularExpression = DecodeString(ref reader);
        var replacement = DnsNameEncoder.Decode(ref reader);
        return new(order, preference, flags, services, regularExpression, replacement);
    }

    protected override DnsRecord<DnsNamingAuthorityPointerRecordData> CreateRecord(DnsName name, DnsNamingAuthorityPointerRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsNamingAuthorityPointerRecord(name, data, @class, ttl);
    }

    protected override void ValidateData(DnsNamingAuthorityPointerRecordData data)
    {
        DnsNamingAuthorityPointerRecordData.Validate(data);
        var stringsLength = DnsNamingAuthorityPointerRecordData.TextEncoding.GetByteCount(data.Flags)
                          + DnsNamingAuthorityPointerRecordData.TextEncoding.GetByteCount(data.Services)
                          + DnsNamingAuthorityPointerRecordData.TextEncoding.GetByteCount(data.RegularExpression);
        ValidateDataLength(checked(7 + stringsLength + GetUncompressedLength(data.Replacement)));
    }

    private static void EncodeString(ref DnsWriter writer, string value)
    {
        var length = DnsNamingAuthorityPointerRecordData.TextEncoding.GetByteCount(value);
        writer.Write((byte)length);
        DnsNamingAuthorityPointerRecordData.TextEncoding.GetBytes(value, writer.ProvideBufferAndAdvance((ushort)length));
    }

    private static string DecodeString(ref DnsReader reader)
    {
        return DnsNamingAuthorityPointerRecordData.TextEncoding.GetString(reader.Read(reader.Read<byte>()));
    }
}