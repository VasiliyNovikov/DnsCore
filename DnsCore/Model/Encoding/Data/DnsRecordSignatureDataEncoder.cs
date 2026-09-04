using System;

using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal sealed class DnsRecordSignatureDataEncoder : DnsRecordDataEncoder<DnsSignatureRecordData>
{
    private const byte PrivateAlgorithm = 253;

    public static readonly DnsRecordSignatureDataEncoder Instance = new();

    protected override void EncodeData(ref DnsWriter writer, DnsSignatureRecordData data)
    {
        writer.Write((ushort)data.TypeCovered);
        writer.Write(data.Algorithm);
        writer.Write(data.Labels);
        writer.WriteTime(data.OriginalTtl);
        writer.Write(data.SignatureExpiration);
        writer.Write(data.SignatureInception);
        writer.Write(data.KeyTag);
        DnsNameEncoder.Encode(ref writer, data.SignerName, false);
        if (data.PrivateAlgorithmName is { } privateAlgorithmName)
            DnsNameEncoder.Encode(ref writer, privateAlgorithmName, false);
        writer.Write(data.Signature);
    }

    protected override DnsSignatureRecordData DecodeData(ref DnsReader reader)
    {
        var typeCovered = (DnsRecordType)reader.Read<ushort>();
        var algorithm = reader.Read<byte>();
        var labels = reader.Read<byte>();
        var originalTtl = reader.ReadTime();
        var signatureExpiration = reader.Read<uint>();
        var signatureInception = reader.Read<uint>();
        var keyTag = reader.Read<ushort>();
        var signerName = DnsNameEncoder.Decode(ref reader);
        var privateAlgorithmName = algorithm == PrivateAlgorithm ? DnsNameEncoder.Decode(ref reader) : null;
        var signature = reader.ReadToEnd().ToArray();
        return new(typeCovered, algorithm, labels, originalTtl, signatureExpiration, signatureInception, keyTag, signerName, privateAlgorithmName, signature);
    }

    protected override DnsRecord<DnsSignatureRecordData> CreateRecord(DnsName name, DnsSignatureRecordData data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    {
        return new DnsSignatureRecord(name, data, @class, ttl);
    }

    protected override void ValidateData(DnsSignatureRecordData data)
    {
        DnsSignatureRecordData.Validate(data);
        var privateAlgorithmLength = data.PrivateAlgorithmName is null ? 0 : GetUncompressedLength(data.PrivateAlgorithmName);
        ValidateDataLength(checked(18 + GetUncompressedLength(data.SignerName) + privateAlgorithmLength + data.Signature.Length));
    }
}