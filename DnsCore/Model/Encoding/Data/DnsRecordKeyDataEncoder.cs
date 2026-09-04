using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordKeyDataEncoder : DnsRecordDataEncoder<DnsKeyRecordData>
{
    private const byte PrivateAlgorithm = 253;

    public static readonly DnsRecordKeyDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsKeyRecordData data)
    {
        writer.Write(data.Flags);
        writer.Write(data.Protocol);
        writer.Write(data.Algorithm);
        if (data.PrivateAlgorithmName is { } privateAlgorithmName)
            DnsNameEncoder.Encode(ref writer, privateAlgorithmName, false);
        writer.Write(data.PublicKey);
    }

    protected override DnsKeyRecordData DecodeData(ref DnsReader reader)
    {
        var flags = reader.Read<ushort>();
        var protocol = reader.Read<byte>();
        var algorithm = reader.Read<byte>();
        DnsName? privateAlgorithmName = null;
        if (algorithm == PrivateAlgorithm && reader.RemainingLength != 0)
            privateAlgorithmName = DnsNameEncoder.Decode(ref reader);
        var publicKey = reader.ReadToEnd().ToArray();
        return new(flags, protocol, algorithm, privateAlgorithmName, publicKey);
    }

    protected override DnsRecord<DnsKeyRecordData> CreateRecord(DnsName name, DnsKeyRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsKeyRecord(name, data, @class, ttl);
    }

    protected override void ValidateData(DnsKeyRecordData data)
    {
        DnsKeyRecordData.Validate(data);
        var privateAlgorithmLength = data.PrivateAlgorithmName is null ? 0 : GetUncompressedLength(data.PrivateAlgorithmName);
        ValidateDataLength(checked(4 + privateAlgorithmLength + data.PublicKey.Length));
    }
}