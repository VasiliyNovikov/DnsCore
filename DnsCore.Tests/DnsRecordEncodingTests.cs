using System;
using System.Net;

using DnsCore.Model;
using DnsCore.Model.Encoding;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsRecordEncodingTests
{
    private static readonly TimeSpan RecordTtl = TimeSpan.FromSeconds(42);
    private static readonly DnsName ExampleName = DnsName.Parse("example.com");

    [TestMethod]
    public void GeneralNames_RealWorldRecords_RoundTripExactWireBytes()
    {
        var mailbox = DnsName.Parse("john+ops.example.com");
        var instance = DnsName.Parse("Office-Printer(Color)._ipp._tcp.local");
        var server = DnsName.ParseHostName("printer.local");
        var reverse = DnsName.Parse("129.2.0.192.in-addr.arpa");
        DnsRecord[] records = [
            new DnsCNameRecord(reverse, DnsName.Parse("129.128/26.2.0.192.in-addr.arpa"), RecordTtl),
            new DnsStartOfAuthorityRecord(ExampleName, DnsName.ParseHostName("ns.example.com"), mailbox, 1, RecordTtl, RecordTtl, RecordTtl, RecordTtl, RecordTtl),
            new DnsPtrRecord(DnsName.Parse("_ipp._tcp.local"), instance, RecordTtl),
            new DnsServiceRecord(instance, 0, 0, 631, server, RecordTtl),
            new DnsAddressRecord(DnsName.Parse("*.example.com"), IPAddress.Loopback, RecordTtl),
            new DnsAddressRecord(DnsName.Parse("service/name.example.com"), IPAddress.IPv6Loopback, RecordTtl),
            new DnsCNameRecord(DnsName.Parse("_acme-challenge.example.com"), DnsName.Parse("validation.example.net"), RecordTtl)
        ];
        var response = new DnsResponse(42, answers: records);
        Span<byte> packet = stackalloc byte[2048];
        var length = DnsResponseEncoder.Encode(packet, response);
        var decoded = DnsResponseEncoder.Decode(packet[..length]);
        for (var i = 0; i < records.Length; ++i)
            DnsAssert.AreEqual(records[i], decoded.Answers[i]);
        Span<byte> encoded = stackalloc byte[2048];
        var encodedLength = DnsResponseEncoder.Encode(encoded, decoded);
        Assert.IsTrue(packet[..length].SequenceEqual(encoded[..encodedLength]));
    }

    [TestMethod]
    public void Test_Encode_Decode_NewRecordTypes()
    {
        var mailName = DnsName.Parse("mail.example.com");
        var errorMailboxName = DnsName.Parse("errors.example.com");
        var nameServerName = DnsName.Parse("ns.example.com");
        var responsibleMailboxName = DnsName.Parse("hostmaster.example.com");
        var targetName = DnsName.Parse("target.example.net");
        DnsRecord[] records = [
            new DnsNameServerRecord(ExampleName, nameServerName, RecordTtl),
            new DnsMailDestinationRecord(ExampleName, mailName, RecordTtl),
            new DnsMailForwarderRecord(ExampleName, mailName, RecordTtl),
            new DnsDNameRecord(ExampleName, targetName, RecordTtl),
            new DnsMailboxRecord(ExampleName, mailName, RecordTtl),
            new DnsMailGroupRecord(ExampleName, mailName, RecordTtl),
            new DnsMailRenameRecord(ExampleName, mailName, RecordTtl),
            new DnsMailInformationRecord(ExampleName, mailName, errorMailboxName, RecordTtl),
            new DnsMailExchangeRecord(ExampleName, 10, mailName, RecordTtl),
            new DnsStartOfAuthorityRecord(ExampleName, nameServerName, responsibleMailboxName, 1, TimeSpan.FromSeconds(62), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5), RecordTtl)
        ];
        var response = new DnsResponse(42, answers: records);
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize];

        var length = DnsResponseEncoder.Encode(buffer, response);
        var actualResponse = DnsResponseEncoder.Decode(buffer[..length]);

        Assert.HasCount(records.Length, actualResponse.Answers);
        for (var i = 0; i < records.Length; ++i)
        {
            Assert.AreEqual(records[i].GetType(), actualResponse.Answers[i].GetType());
            DnsAssert.AreEqual(records[i], actualResponse.Answers[i]);
        }
    }

    [TestMethod]
    public void Test_Create_RawRecord_ForCompressionCapableType_Throws()
    {
        DnsRecordType[] dangerousTypes = [
            DnsRecordType.NS,
            DnsRecordType.MD,
            DnsRecordType.MF,
            DnsRecordType.CNAME,
            DnsRecordType.SOA,
            DnsRecordType.MB,
            DnsRecordType.MG,
            DnsRecordType.MR,
            DnsRecordType.PTR,
            DnsRecordType.MINFO,
            DnsRecordType.MX,
            DnsRecordType.RP,
            DnsRecordType.AFSDB,
            DnsRecordType.RT,
            DnsRecordType.SIG,
            DnsRecordType.KEY,
            DnsRecordType.PX,
            DnsRecordType.NXT,
            DnsRecordType.SRV,
            DnsRecordType.NAPTR,
            DnsRecordType.DNAME
        ];

        foreach (var dangerousType in dangerousTypes)
            Assert.ThrowsExactly<ArgumentException>(() => new DnsRawRecord(ExampleName, [], dangerousType, DnsClass.IN, RecordTtl));
    }

    [TestMethod]
    public void Test_Encode_CustomByteRecord_Throws()
    {
        var record = new CustomByteRecord(ExampleName, [0xC0, 0x0C], (DnsRecordType)65400, DnsClass.IN, RecordTtl);
        var response = new DnsResponse(42, answers: [record]);
        var buffer = new byte[DnsDefaults.MaxUdpMessageSize];

        Assert.ThrowsExactly<NotSupportedException>(() => DnsResponseEncoder.Encode(buffer, response));
    }

    [TestMethod]
    public void Test_Encode_Decode_SafeRawRecord()
    {
        byte[] data = [0xC0, 0x0C, 0xFF];
        var expected = new DnsRawRecord(ExampleName, data, (DnsRecordType)65400, DnsClass.HS, RecordTtl);
        var response = new DnsResponse(42, answers: [expected]);
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize];

        var length = DnsResponseEncoder.Encode(buffer, response);
        var actualResponse = DnsResponseEncoder.Decode(buffer[..length]);

        DnsAssert.AreEqual(expected, actualResponse.Answers[0]);
    }

    private sealed class CustomByteRecord(DnsName name, byte[] data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
        : DnsRecord<byte[]>(name, data, recordType, @class, ttl);
}