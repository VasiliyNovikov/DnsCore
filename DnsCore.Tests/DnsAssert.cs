using DnsCore.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

internal static class DnsAssert
{
    public static void AreEqual(DnsRecord expected, DnsRecord actual)
    {
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.RecordType, actual.RecordType);
        Assert.AreEqual(expected.Class, actual.Class);
        Assert.AreEqual(expected.Ttl, actual.Ttl);
        switch (expected.RecordType)
        {
            case DnsRecordType.A:
            case DnsRecordType.AAAA:
                Assert.IsTrue(((DnsAddressRecord)expected).Data.Equals(((DnsAddressRecord)actual).Data));
                break;
            case DnsRecordType.CNAME:
            case DnsRecordType.NS:
            case DnsRecordType.MD:
            case DnsRecordType.MF:
            case DnsRecordType.MB:
            case DnsRecordType.MG:
            case DnsRecordType.MR:
            case DnsRecordType.PTR:
            case DnsRecordType.DNAME:
                Assert.AreEqual(((DnsNameRecord)expected).Data, ((DnsNameRecord)actual).Data);
                break;
            case DnsRecordType.SOA:
                Assert.AreEqual(((DnsStartOfAuthorityRecord)expected).Data, ((DnsStartOfAuthorityRecord)actual).Data);
                break;
            case DnsRecordType.MINFO:
                Assert.AreEqual(((DnsMailInformationRecord)expected).Data, ((DnsMailInformationRecord)actual).Data);
                break;
            case DnsRecordType.MX:
                Assert.AreEqual(((DnsMailExchangeRecord)expected).Data, ((DnsMailExchangeRecord)actual).Data);
                break;
            case DnsRecordType.TXT:
                Assert.AreEqual(((DnsTextRecord)expected).Data, ((DnsTextRecord)actual).Data);
                break;
            case DnsRecordType.SRV:
                Assert.AreEqual(((DnsServiceRecord)expected).Data, ((DnsServiceRecord)actual).Data);
                break;
            default:
                Assert.AreSequenceEqual(((DnsRawRecord)expected).Data, ((DnsRawRecord)actual).Data);
                break;
        }
    }
}