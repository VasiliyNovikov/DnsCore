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
            case DnsRecordType.PTR:
                Assert.AreEqual(((DnsNameRecord)expected).Data, ((DnsNameRecord)actual).Data);
                break;
            case DnsRecordType.TXT:
                Assert.AreEqual(((DnsTextRecord)expected).Data, ((DnsTextRecord)actual).Data);
                break;
            default:
                CollectionAssert.AreEqual(((DnsRawRecord)expected).Data, ((DnsRawRecord)actual).Data);
                break;
        }
    }
}