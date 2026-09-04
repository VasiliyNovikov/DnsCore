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
            case DnsRecordType.RP:
                Assert.AreEqual(((DnsResponsiblePersonRecord)expected).Data, ((DnsResponsiblePersonRecord)actual).Data);
                break;
            case DnsRecordType.AFSDB:
                Assert.AreEqual(((DnsAfsDatabaseRecord)expected).Data, ((DnsAfsDatabaseRecord)actual).Data);
                break;
            case DnsRecordType.RT:
                Assert.AreEqual(((DnsRouteThroughRecord)expected).Data, ((DnsRouteThroughRecord)actual).Data);
                break;
            case DnsRecordType.PX when expected is DnsMailMappingRecord expectedMailMapping:
                Assert.AreEqual(expectedMailMapping.Data, ((DnsMailMappingRecord)actual).Data);
                break;
            case DnsRecordType.NAPTR:
                Assert.AreEqual(((DnsNamingAuthorityPointerRecord)expected).Data, ((DnsNamingAuthorityPointerRecord)actual).Data);
                break;
            case DnsRecordType.NXT:
                Assert.AreEqual(((DnsNextDomainRecord)expected).Data.NextDomainName, ((DnsNextDomainRecord)actual).Data.NextDomainName);
                Assert.AreSequenceEqual(((DnsNextDomainRecord)expected).Data.TypeBitmap, ((DnsNextDomainRecord)actual).Data.TypeBitmap);
                break;
            case DnsRecordType.SIG:
                AssertSignatureDataEqual(((DnsSignatureRecord)expected).Data, ((DnsSignatureRecord)actual).Data);
                break;
            case DnsRecordType.KEY:
                AssertKeyDataEqual(((DnsKeyRecord)expected).Data, ((DnsKeyRecord)actual).Data);
                break;
            default:
                Assert.AreSequenceEqual(((DnsRawRecord)expected).Data, ((DnsRawRecord)actual).Data);
                break;
        }
    }

    private static void AssertSignatureDataEqual(DnsSignatureRecordData expected, DnsSignatureRecordData actual)
    {
        Assert.AreEqual(expected.TypeCovered, actual.TypeCovered);
        Assert.AreEqual(expected.Algorithm, actual.Algorithm);
        Assert.AreEqual(expected.Labels, actual.Labels);
        Assert.AreEqual(expected.OriginalTtl, actual.OriginalTtl);
        Assert.AreEqual(expected.SignatureExpiration, actual.SignatureExpiration);
        Assert.AreEqual(expected.SignatureInception, actual.SignatureInception);
        Assert.AreEqual(expected.KeyTag, actual.KeyTag);
        Assert.AreEqual(expected.SignerName, actual.SignerName);
        Assert.AreEqual(expected.PrivateAlgorithmName, actual.PrivateAlgorithmName);
        Assert.AreSequenceEqual(expected.Signature, actual.Signature);
    }

    private static void AssertKeyDataEqual(DnsKeyRecordData expected, DnsKeyRecordData actual)
    {
        Assert.AreEqual(expected.Flags, actual.Flags);
        Assert.AreEqual(expected.Protocol, actual.Protocol);
        Assert.AreEqual(expected.Algorithm, actual.Algorithm);
        Assert.AreEqual(expected.PrivateAlgorithmName, actual.PrivateAlgorithmName);
        Assert.AreSequenceEqual(expected.PublicKey, actual.PublicKey);
    }
}