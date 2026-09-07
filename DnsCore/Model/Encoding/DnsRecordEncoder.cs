using System;
using System.Buffers.Binary;

using DnsCore.IO;
using DnsCore.Model.Encoding.Data;

namespace DnsCore.Model.Encoding;

internal static class DnsRecordEncoder
{
    private static readonly DnsRecordDataEncoder DefaultEncoder;
    private static readonly EncoderRegistration?[] Encoders;

    static DnsRecordEncoder()
    {
        DefaultEncoder = DnsRecordRawDataEncoder.Instance;
        Encoders = new EncoderRegistration?[UInt16.MaxValue + 1];
        RegisterTypeEncoder(DnsRecordType.A, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.AAAA, DnsRecordAddressDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.NS, DnsRecordNameServerDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MD, DnsRecordMailDestinationDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MF, DnsRecordMailForwarderDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.CNAME, DnsRecordCNameDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.SOA, DnsRecordStartOfAuthorityDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MB, DnsRecordMailboxDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MG, DnsRecordMailGroupDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MR, DnsRecordMailRenameDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.PTR, DnsRecordPtrDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MINFO, DnsRecordMailInformationDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.MX, DnsRecordMailExchangeDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.SRV, DnsRecordServiceDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.DNAME, DnsRecordDNameDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.TXT, DnsRecordTextDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.RP, DnsRecordResponsiblePersonDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.AFSDB, DnsRecordAfsDatabaseDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.RT, DnsRecordRouteThroughDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.SIG, DnsRecordSignatureDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.KEY, DnsRecordKeyDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.PX, DnsRecordMailMappingDataEncoder.Instance, DnsClass.IN);
        RegisterTypeEncoder(DnsRecordType.NXT, DnsRecordNextDomainDataEncoder.Instance);
        RegisterTypeEncoder(DnsRecordType.NAPTR, DnsRecordNamingAuthorityPointerDataEncoder.Instance);
    }

    public static void RegisterTypeEncoder(DnsRecordType type, DnsRecordDataEncoder encoder) => Encoders[(ushort)type] = new(encoder, null);

    public static void RegisterTypeEncoder(DnsRecordType type, DnsRecordDataEncoder encoder, params DnsClass[] supportedClasses)
    {
        if (supportedClasses.Length == 0)
            throw new ArgumentException("At least one supported class must be specified", nameof(supportedClasses));
        Encoders[(ushort)type] = new(encoder, [.. supportedClasses]);
    }

    private static DnsRecordDataEncoder GetEncoder(DnsRecordType type, DnsClass @class)
    {
        var registration = Encoders[(ushort)type];
        return registration is not null && registration.Supports(@class) ? registration.Encoder : DefaultEncoder;
    }

    public static void Encode(ref DnsWriter writer, DnsRecord record)
    {
        var encoder = GetEncoder(record.RecordType, record.Class);
        encoder.Validate(record);
        DnsRecordBaseEncoder.Encode(ref writer, record);
        writer.WriteTime(record.Ttl);

        var dataLenBuffer = writer.ProvideBufferAndAdvance(2);

        var dataPosition = writer.Position;
        encoder.Encode(ref writer, record);

        BinaryPrimitives.WriteUInt16BigEndian(dataLenBuffer, (ushort)(writer.Position - dataPosition));
    }

    public static DnsRecord Decode(ref DnsReader reader)
    {
        var (name, type, @class) = DnsRecordBaseEncoder.Decode(ref reader);
        var ttl = reader.ReadTime();
        var dataLength = reader.Read<ushort>();
        var dataReader = reader.GetSubReader(reader.Position, dataLength);
        reader.Skip(dataLength);
        return GetEncoder(type, @class).Decode(ref dataReader, name, type, @class, ttl);
    }

    private sealed record EncoderRegistration(DnsRecordDataEncoder Encoder, DnsClass[]? SupportedClasses)
    {
        public bool Supports(DnsClass @class) => SupportedClasses is null || Array.IndexOf(SupportedClasses, @class) >= 0;
    }
}
