using System;
using System.Buffers.Binary;

using DnsCore.IO;
using DnsCore.Model;
using DnsCore.Model.Encoding;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsRecordEncodingTests
{
    private static readonly TimeSpan RecordTtl = TimeSpan.FromSeconds(42);
    private static readonly DnsName ExampleName = DnsName.Parse("example.com");
    private static readonly DnsName MailName = DnsName.Parse("mail.example.com");
    private static readonly DnsName OtherName = DnsName.Parse("other.example.com");

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
    public void Test_Encode_Decode_CompressionRejectedRecordTypes()
    {
        DnsRecord[] records = [
            new DnsResponsiblePersonRecord(ExampleName, MailName, OtherName, RecordTtl),
            new DnsAfsDatabaseRecord(ExampleName, 1, MailName, RecordTtl),
            new DnsRouteThroughRecord(ExampleName, 10, MailName, RecordTtl),
            new DnsSignatureRecord(ExampleName, DnsRecordType.A, 8, 2, RecordTtl, 2_000_000_000, 1_900_000_000, 1234, ExampleName, null, [1, 2, 3], RecordTtl),
            new DnsKeyRecord(ExampleName, 0, 3, 8, null, [4, 5, 6], RecordTtl),
            new DnsMailMappingRecord(ExampleName, 50, MailName, OtherName, RecordTtl),
            new DnsNextDomainRecord(ExampleName, OtherName, [0x00, 0x00, 0x00, 0x02], RecordTtl),
            new DnsNamingAuthorityPointerRecord(ExampleName, 100, 10, "S", "SIP+D2U", "", MailName, RecordTtl),
            new DnsDNameRecord(ExampleName, OtherName, RecordTtl)
        ];

        var response = new DnsResponse(42, answers: records);
        Span<byte> buffer = stackalloc byte[2048];
        var length = DnsResponseEncoder.Encode(buffer, response);
        var actual = DnsResponseEncoder.Decode(buffer[..length]);

        Assert.HasCount(records.Length, actual.Answers);
        for (var i = 0; i < records.Length; ++i)
        {
            Assert.AreEqual(records[i].GetType(), actual.Answers[i].GetType());
            DnsAssert.AreEqual(records[i], actual.Answers[i]);
        }
    }

    [TestMethod]
    public void Test_Encode_NewRecordTypes_UsesExpectedWireData()
    {
        var example = "\x0007example\x0003com\0"u8.ToArray();
        var mail = "\x0004mail\x0007example\x0003com\0"u8.ToArray();
        var other = "\x0005other\x0007example\x0003com\0"u8.ToArray();
        var cases = new (DnsRecord Record, byte[] Data)[]
        {
            (new DnsResponsiblePersonRecord(ExampleName, MailName, OtherName, RecordTtl), [.. mail, .. other]),
            (new DnsAfsDatabaseRecord(ExampleName, 1, MailName, RecordTtl), [0, 1, .. mail]),
            (new DnsRouteThroughRecord(ExampleName, 10, MailName, RecordTtl), [0, 10, .. mail]),
            (new DnsMailMappingRecord(ExampleName, 50, MailName, OtherName, RecordTtl), [0, 50, .. mail, .. other]),
            (new DnsNextDomainRecord(ExampleName, OtherName, [0, 0, 0, 2], RecordTtl), [.. other, 0, 0, 0, 2]),
            (new DnsNamingAuthorityPointerRecord(ExampleName, 100, 10, "S", "SIP+D2U", "", MailName, RecordTtl), [0, 100, 0, 10, 1, (byte)'S', 7, (byte)'S', (byte)'I', (byte)'P', (byte)'+', (byte)'D', (byte)'2', (byte)'U', 0, .. mail]),
            (new DnsDNameRecord(ExampleName, OtherName, RecordTtl), other),
            (new DnsSignatureRecord(ExampleName, DnsRecordType.A, 8, 2, RecordTtl, 0x01020304, 0x05060708, 0x090A, ExampleName, null, [0xAA], RecordTtl), [0, 1, 8, 2, 0, 0, 0, 42, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, .. example, 0xAA]),
            (new DnsKeyRecord(ExampleName, 0x0102, 3, 8, null, [0xAA, 0xBB], RecordTtl), [1, 2, 3, 8, 0xAA, 0xBB])
        };

        foreach (var (record, expectedData) in cases)
            Assert.AreSequenceEqual(expectedData, EncodeRecordData(record), record.RecordType.ToString());
    }

    [TestMethod]
    public void Test_Decode_HistoricallyCompressedData_ReencodesUncompressed()
    {
        var pointer = new byte[] { 0xC0, 0x0C };
        var fixedSignatureData = new byte[] { 0, 1, 8, 2, 0, 0, 0, 42, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var cases = new (DnsRecordType Type, byte[] CompressedData)[]
        {
            (DnsRecordType.RP, [.. pointer, .. pointer]),
            (DnsRecordType.AFSDB, [0, 1, .. pointer]),
            (DnsRecordType.RT, [0, 10, .. pointer]),
            (DnsRecordType.SIG, [.. fixedSignatureData, .. pointer, 0xAA]),
            (DnsRecordType.PX, [0, 50, .. pointer, .. pointer]),
            (DnsRecordType.NXT, [.. pointer, 0x80]),
            (DnsRecordType.NAPTR, [0, 1, 0, 2, 0, 0, 0, .. pointer]),
            (DnsRecordType.DNAME, pointer)
        };
        var uncompressedName = "\x0007example\x0003com\0"u8;

        foreach (var (type, compressedData) in cases)
        {
            var decoded = DecodeRecord(type, DnsClass.IN, compressedData);
            var encodedData = EncodeRecordData(decoded);

            Assert.IsFalse(encodedData.AsSpan().SequenceEqual(compressedData), type.ToString());
            Assert.IsGreaterThanOrEqualTo(0, encodedData.AsSpan().IndexOf(uncompressedName), type.ToString());
        }
    }

    [TestMethod]
    public void Test_Encode_Decode_PrivateAlgorithmRecords()
    {
        var privateAlgorithmName = DnsName.Parse("algorithm.example.com");
        DnsRecord[] records = [
            new DnsSignatureRecord(ExampleName, DnsRecordType.A, 253, 2, RecordTtl, 100, 50, 7, ExampleName, privateAlgorithmName, [1, 2], RecordTtl),
            new DnsKeyRecord(ExampleName, 0, 3, 253, privateAlgorithmName, [3, 4], RecordTtl),
            new DnsKeyRecord(ExampleName, 0xC000, 3, 253, null, [], RecordTtl)
        ];

        foreach (var expected in records)
        {
            var actual = DecodeRecord(expected.RecordType, expected.Class, EncodeRecordData(expected));
            DnsAssert.AreEqual(expected, actual);
        }
    }

    [TestMethod]
    public void Test_Decode_PrivateKeyAlgorithmWithoutIdentifier_Throws()
    {
        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(CreateEncodedResponse(DnsRecordType.KEY, DnsClass.IN, [0, 0, 3, 253])));
    }

    [TestMethod]
    public void Test_Create_InvalidRecordData_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new DnsNextDomainRecordData(OtherName, []));
        Assert.ThrowsExactly<ArgumentException>(() => new DnsSignatureRecordData(DnsRecordType.A, 8, 2, RecordTtl, 100, 50, 7, ExampleName, null, []));
        Assert.ThrowsExactly<ArgumentException>(() => new DnsKeyRecordData(0, 3, 253, null, []));
        Assert.ThrowsExactly<ArgumentException>(() => new DnsKeyRecordData(0xC000, 3, 8, null, [1]));
        Assert.ThrowsExactly<ArgumentException>(() => new DnsKeyRecordData(0xC000, 3, 253, OtherName, []));
    }

    [TestMethod]
    public void Test_Decode_NoKeyRecordWithTrailingData_Throws()
    {
        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(CreateEncodedResponse(DnsRecordType.KEY, DnsClass.IN, [0xC0, 0, 3, 8, 1])));
        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(CreateEncodedResponse(DnsRecordType.KEY, DnsClass.IN, [0xC0, 0, 3, 253, 0])));
    }

    [TestMethod]
    [DataRow(DnsRecordType.SIG)]
    [DataRow(DnsRecordType.KEY)]
    public void Test_Decode_PrivateAlgorithmNameCompressed_ReencodesUncompressed(DnsRecordType type)
    {
        byte[] privateData = [0xC0, 0xFF];
        var compressedPrivateAlgorithmName = "\x0009algorithm"u8.ToArray();
        compressedPrivateAlgorithmName = [.. compressedPrivateAlgorithmName, 0xC0, 0x0C];
        byte[] data = type switch
        {
            DnsRecordType.SIG => [0, 1, 253, 2, 0, 0, 0, 42, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0xC0, 0x0C, .. compressedPrivateAlgorithmName, .. privateData],
            DnsRecordType.KEY => [0, 0, 3, 253, .. compressedPrivateAlgorithmName, .. privateData],
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var decoded = DecodeRecord(type, DnsClass.IN, data);
        var expectedPrivateAlgorithmName = DnsName.Parse("algorithm.example.com");
        byte[] expectedData = type switch
        {
            DnsRecordType.SIG => [0, 1, 253, 2, 0, 0, 0, 42, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, .. "\x0007example\x0003com\0"u8, .. "\x0009algorithm\x0007example\x0003com\0"u8, .. privateData],
            DnsRecordType.KEY => [0, 0, 3, 253, .. "\x0009algorithm\x0007example\x0003com\0"u8, .. privateData],
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (decoded is DnsSignatureRecord signature)
        {
            Assert.AreEqual(expectedPrivateAlgorithmName, signature.Data.PrivateAlgorithmName);
            Assert.AreSequenceEqual(privateData, signature.Data.Signature);
        }
        else
        {
            var key = (DnsKeyRecord)decoded;
            Assert.AreEqual(expectedPrivateAlgorithmName, key.Data.PrivateAlgorithmName);
            Assert.AreSequenceEqual(privateData, key.Data.PublicKey);
        }
        Assert.AreSequenceEqual(expectedData, EncodeRecordData(decoded));
    }

    [TestMethod]
    public void Test_Decode_NonInternetPx_PreservesOpaqueData()
    {
        byte[] opaqueData = [0xC0, 0x0C, 0xFF];
        var encoded = CreateEncodedResponse(DnsRecordType.PX, DnsClass.HS, opaqueData);

        var response = DnsResponseEncoder.Decode(encoded);
        var actual = Assert.IsInstanceOfType<DnsRawRecord>(response.Answers[0]);
        Assert.AreEqual(DnsClass.HS, actual.Class);
        Assert.AreSequenceEqual(opaqueData, actual.Data);
        Assert.AreSequenceEqual(opaqueData, EncodeRecordData(actual));
        DnsAssert.AreEqual(new DnsRawRecord(ExampleName, opaqueData, DnsRecordType.PX, DnsClass.HS, RecordTtl), actual);
    }

    [TestMethod]
    public void Test_Encode_TypedNonInternetPx_Throws()
    {
        var record = new CustomRecord<DnsMailMappingRecordData>(ExampleName, new(50, MailName, OtherName), DnsRecordType.PX, DnsClass.HS, RecordTtl);
        var buffer = new byte[512];

        Assert.ThrowsExactly<NotSupportedException>(() => DnsResponseEncoder.Encode(buffer, new DnsResponse(1, answers: [record])));
    }

    [TestMethod]
    public void Test_Decode_NewTypedRecords_PreservesClass()
    {
        DnsRecord[] records = [
            new DnsResponsiblePersonRecord(ExampleName, MailName, OtherName, DnsClass.HS, RecordTtl),
            new DnsAfsDatabaseRecord(ExampleName, 1, MailName, DnsClass.HS, RecordTtl),
            new DnsRouteThroughRecord(ExampleName, 10, MailName, DnsClass.HS, RecordTtl),
            new DnsNamingAuthorityPointerRecord(ExampleName, new(1, 2, "", "", "", MailName), DnsClass.HS, RecordTtl),
            new DnsNextDomainRecord(ExampleName, new(OtherName, [0x80]), DnsClass.HS, RecordTtl),
            new DnsSignatureRecord(ExampleName, new(DnsRecordType.A, 8, 2, RecordTtl, 100, 50, 7, ExampleName, null, [1]), DnsClass.HS, RecordTtl),
            new DnsKeyRecord(ExampleName, new(0, 3, 8, null, [1]), DnsClass.HS, RecordTtl),
            new DnsDNameRecord(ExampleName, OtherName, DnsClass.HS, RecordTtl)
        ];

        foreach (var expected in records)
        {
            var actual = DecodeRecord(expected.RecordType, expected.Class, EncodeRecordData(expected));
            Assert.AreEqual(DnsClass.HS, actual.Class);
            DnsAssert.AreEqual(expected, actual);
        }
    }

    [TestMethod]
    public void Test_Create_Naptr_WithOversizedString_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DnsNamingAuthorityPointerRecord(ExampleName, 1, 2, new string('a', 256), "", "", OtherName, RecordTtl));
        _ = new DnsNamingAuthorityPointerRecord(ExampleName, 1, 2, new string('\u00E9', 127), "", "", OtherName, RecordTtl);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DnsNamingAuthorityPointerRecord(ExampleName, 1, 2, new string('\u00E9', 128), "", "", OtherName, RecordTtl));
    }

    [TestMethod]
    public void Test_Decode_Naptr_WithInvalidUtf8_Throws()
    {
        byte[] data = [0, 1, 0, 2, 1, 0xFF, 0, 0, 0];
        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(CreateEncodedResponse(DnsRecordType.NAPTR, DnsClass.IN, data)));
    }

    [TestMethod]
    [DataRow(DnsRecordType.NXT)]
    [DataRow(DnsRecordType.SIG)]
    [DataRow(DnsRecordType.KEY)]
    public void Test_Encode_InvalidNewRecordData_Throws(DnsRecordType type)
    {
        DnsRecord record = type switch
        {
            DnsRecordType.NXT => new CustomRecord<DnsNextDomainRecordData>(ExampleName, default, type, RecordTtl),
            DnsRecordType.SIG => new CustomRecord<DnsSignatureRecordData>(ExampleName, default, type, RecordTtl),
            DnsRecordType.KEY => new CustomRecord<DnsKeyRecordData>(ExampleName, default, type, RecordTtl),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var buffer = new byte[512];

        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Encode(buffer, new DnsResponse(1, answers: [record])));
    }

    [TestMethod]
    public void Test_Encode_BufferLargerThanDnsMaximum_DoesNotWrap()
    {
        var buffer = new byte[70_000];
        var response = new DnsResponse(1, answers: [new DnsDNameRecord(ExampleName, OtherName, RecordTtl)]);

        var length = DnsResponseEncoder.Encode(buffer, response);

        Assert.IsGreaterThan((ushort)0, length);
        _ = DnsResponseEncoder.Decode(buffer.AsSpan(0, length));
        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(buffer));
    }

    [TestMethod]
    public void Test_Encode_NameIntroducedAboveCompressionOffsetLimit_RoundTrips()
    {
        var lateName = DnsName.Parse("late.example.com");
        var response = new DnsResponse(1, answers: [
            new DnsKeyRecord(ExampleName, 0, 3, 8, null, new byte[17_000], RecordTtl),
            new DnsDNameRecord(lateName, OtherName, RecordTtl),
            new DnsDNameRecord(lateName, MailName, RecordTtl)
        ]);
        var buffer = new byte[UInt16.MaxValue];

        var length = DnsResponseEncoder.Encode(buffer, response);
        var actual = DnsResponseEncoder.Decode(buffer.AsSpan(0, length));

        Assert.AreEqual(lateName, actual.Answers[1].Name);
        Assert.AreEqual(lateName, actual.Answers[2].Name);
    }

    [TestMethod]
    public void Test_Encode_RecordDataAboveMaximum_ThrowsBeforeWritingRecord()
    {
        var response = new DnsResponse(1, answers: [new DnsKeyRecord(ExampleName, 0, 3, 8, null, new byte[UInt16.MaxValue - 3], RecordTtl)]);
        var buffer = new byte[70_000];

        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Encode(buffer, response));
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

    private sealed class CustomRecord<T>(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
        : DnsRecord<T>(name, data, recordType, @class, ttl) where T : notnull
    {
        public CustomRecord(DnsName name, T data, DnsRecordType recordType, TimeSpan ttl)
            : this(name, data, recordType, DnsClass.IN, ttl) { }
    }

    private static byte[] EncodeRecordData(DnsRecord record)
    {
        var buffer = new byte[UInt16.MaxValue];
        var length = DnsResponseEncoder.Encode(buffer, new DnsResponse(1, answers: [record]));
        var reader = new DnsReader(buffer.AsSpan(0, length));
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = DnsNameEncoder.Decode(ref reader);
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.ReadTime();
        return reader.Read(reader.Read<ushort>()).ToArray();
    }

    private static DnsRecord DecodeRecord(DnsRecordType type, DnsClass @class, byte[] data)
    {
        return DnsResponseEncoder.Decode(CreateEncodedResponse(type, @class, data)).Answers[0];
    }

    private static byte[] CreateEncodedResponse(DnsRecordType type, DnsClass @class, byte[] data)
    {
        var owner = "\x0007example\x0003com\0"u8;
        var buffer = new byte[12 + owner.Length + 10 + data.Length];
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(6, 2), 1);
        owner.CopyTo(buffer.AsSpan(12));
        var position = 12 + owner.Length;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(position, 2), (ushort)type);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(position + 2, 2), (ushort)@class);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(position + 4, 4), 42);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(position + 8, 2), (ushort)data.Length);
        data.CopyTo(buffer.AsSpan(position + 10));
        return buffer;
    }
}