using System;

namespace DnsCore.Model;

public sealed class DnsRawRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord<byte[]>(name, ValidateData(data, recordType, @class), recordType, @class, ttl)
{
    private protected override string DataToString() => BitConverter.ToString(Data);

    private static byte[] ValidateData(byte[] data, DnsRecordType recordType, DnsClass @class)
    {
        return recordType is (DnsRecordType.NS
                           or DnsRecordType.MD
                           or DnsRecordType.MF
                           or DnsRecordType.CNAME
                           or DnsRecordType.SOA
                           or DnsRecordType.MB
                           or DnsRecordType.MG
                           or DnsRecordType.MR
                           or DnsRecordType.PTR
                           or DnsRecordType.MINFO
                           or DnsRecordType.MX
                           or DnsRecordType.RP
                           or DnsRecordType.AFSDB
                           or DnsRecordType.RT
                           or DnsRecordType.SIG
                           or DnsRecordType.KEY
                           or DnsRecordType.PX
                           or DnsRecordType.NXT
                           or DnsRecordType.SRV
                           or DnsRecordType.NAPTR
                           or DnsRecordType.DNAME)
                       && (recordType != DnsRecordType.PX || @class == DnsClass.IN)
            ? throw new ArgumentException($"{recordType} records can contain DNS compression pointers and require type-specific encoding", nameof(recordType))
            : data;
    }
}