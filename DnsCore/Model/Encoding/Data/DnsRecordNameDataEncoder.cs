using DnsCore.IO;

namespace DnsCore.Model.Encoding.Data;

internal abstract class DnsRecordNameDataEncoder : DnsRecordDataEncoder<DnsName>
{
    protected override void EncodeData(ref DnsWriter writer, DnsName data) => DnsNameEncoder.Encode(ref writer, data);
    protected override DnsName DecodeData(ref DnsReader reader) => DnsNameEncoder.Decode(ref reader);
}