using System;
using System.Collections.Generic;
using System.Net;

using DnsCore.IO;
using DnsCore.Model;
using DnsCore.Model.Encoding;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsEncodingTests
{
    [TestMethod]
    public void Test_Encode_Decode()
    {
        List<DnsRequest> requests = [
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.AAAA),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.CNAME),
            new DnsRequest(DnsName.Parse("unknown.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsRecordType.PTR),
            new DnsRequest(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsRecordType.PTR),
            new DnsRequest(DnsName.Parse("_ldap._tcp.example.com"), DnsRecordType.SRV)
        ];

        List<DnsResponse> responses = [
            requests[0].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42))),

            requests[1].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(42))),

            requests[2].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[3].Reply(DnsResponseStatus.NameError),

            requests[4].Reply(new DnsPtrRecord(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[5].Reply(new DnsPtrRecord(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[6].Reply(new DnsServiceRecord(DnsName.Parse("_ldap._tcp.example.com"), 0, 5, 389, DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)))
        ];

        List<DnsMessage> messages = [.. requests, .. responses];

        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize];
        foreach (var message in messages)
        {
            var length = DnsMessageEncoder.Encode(buffer, message);
            var messageSpan = buffer[..length];

            DnsMessage actualMessage = message is DnsRequest ? DnsRequestEncoder.Decode(messageSpan) : DnsResponseEncoder.Decode(messageSpan);

            Assert.AreEqual(message.Id, actualMessage.Id);
            Assert.AreEqual(message.RequestType, actualMessage.RequestType);
            Assert.AreEqual(message.RecursionDesired, actualMessage.RecursionDesired);
            Assert.HasCount(message.Questions.Count, actualMessage.Questions);
            for (var i = 0; i < message.Questions.Count; ++i)
                Assert.AreEqual(message.Questions[i], actualMessage.Questions[i]);

            if (message is DnsRequest)
                Assert.IsInstanceOfType<DnsRequest>(actualMessage);
            else
            {
                var response = (DnsResponse)message;
                var actualResponse = (DnsResponse)actualMessage;
                Assert.AreEqual(response.Status, actualResponse.Status);
                Assert.AreEqual(response.RecursionAvailable, actualResponse.RecursionAvailable);
                Assert.AreEqual(response.AuthoritativeAnswer, actualResponse.AuthoritativeAnswer);
                Assert.AreEqual(response.Truncated, actualResponse.Truncated);
                Assert.HasCount(response.Answers.Count, actualResponse.Answers);
                for (var i = 0; i < response.Answers.Count; ++i)
                    DnsAssert.AreEqual(response.Answers[i], actualResponse.Answers[i]);
                Assert.HasCount(response.Authorities.Count, actualResponse.Authorities);
                for (var i = 0; i < response.Authorities.Count; ++i)
                    DnsAssert.AreEqual(response.Authorities[i], actualResponse.Authorities[i]);
                Assert.HasCount(response.Additional.Count, actualResponse.Additional);
                for (var i = 0; i < response.Additional.Count; ++i)
                    DnsAssert.AreEqual(response.Additional[i], actualResponse.Additional[i]);
            }

            Assert.AreEqual(length, DnsMessageEncoder.Encode(buffer[..length], message));

            for (var l = 0; l < length; ++l)
            {
                var smallLength = l;
                Assert.ThrowsExactly<FormatException>(() =>
                {
                    Span<byte> smallBuffer = stackalloc byte[smallLength];
                    DnsMessageEncoder.Encode(smallBuffer, message);
                });

                Assert.ThrowsExactly<FormatException>(() =>
                {
                    Span<byte> buffer = stackalloc byte[length];
                    DnsMessageEncoder.Encode(buffer, message);

                    var smallBuffer = buffer[..smallLength];
                    if (message is DnsRequest)
                        DnsRequestEncoder.Decode(smallBuffer);
                    else
                        DnsResponseEncoder.Decode(smallBuffer);
                });
            }
        }
    }

    [TestMethod]
    public void Test_Encode_SrvTarget_IsNotCompressed()
    {
        var request = new DnsRequest(DnsName.Parse("_ldap._tcp.example.com"), DnsRecordType.SRV);
        var message = request.Reply(new DnsServiceRecord(DnsName.Parse("_ldap._tcp.example.com"), 0, 5, 389, DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)));
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize];

        var length = DnsMessageEncoder.Encode(buffer, message);
        var messageSpan = buffer[..length];
        var reader = new DnsReader(messageSpan);
        _ = reader.Read<ushort>(); // ID
        _ = reader.Read<ushort>(); // Flags
        var questionCount = reader.Read<ushort>();
        var answerCount = reader.Read<ushort>();
        _ = reader.Read<ushort>(); // Authority count
        _ = reader.Read<ushort>(); // Additional count
        Assert.AreEqual(1, questionCount);
        Assert.AreEqual(1, answerCount);

        _ = DnsNameEncoder.Decode(ref reader);
        _ = reader.Read<ushort>(); // Question type
        _ = reader.Read<ushort>(); // Question class
        _ = DnsNameEncoder.Decode(ref reader);
        _ = reader.Read<ushort>(); // Answer type
        _ = reader.Read<ushort>(); // Answer class
        _ = reader.Read<uint>(); // TTL
        var dataLength = reader.Read<ushort>();
        var data = reader.Read(dataLength);
        byte[] expectedParameters = [0x00, 0x00, 0x00, 0x05, 0x01, 0x85];
        var expectedTarget = "\x0004host\x0007example\x0003com\0"u8;

        Assert.AreEqual(6 + expectedTarget.Length, data.Length);
        Assert.IsTrue(expectedParameters.SequenceEqual(data[..6]));
        Assert.IsTrue(expectedTarget.SequenceEqual(data[6..]));
        Assert.IsFalse(data[6..].Contains((byte)0xC0));
    }

    [TestMethod]
    [DataRow(DnsRecordType.A)]
    [DataRow(DnsRecordType.AAAA)]
    [DataRow(DnsRecordType.CNAME)]
    [DataRow(DnsRecordType.PTR)]
    [DataRow(DnsRecordType.SRV)]
    [DataRow(DnsRecordType.TXT)]
    public void Test_Encode_RawData_ForKnownRecordType_DecodesTypedRecord(DnsRecordType recordType)
    {
        var name = DnsName.Parse("_ldap._tcp.example.com");
        var request = new DnsRequest(name, recordType);
        var expectedTarget = DnsName.Parse("host.example.com");
        ReadOnlyMemory<byte> rawData = recordType switch
        {
            DnsRecordType.A => (byte[])[0x01, 0x02, 0x03, 0x04],
            DnsRecordType.AAAA => [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F],
            DnsRecordType.CNAME or DnsRecordType.PTR => [
                0x04, 0x68, 0x6F, 0x73, 0x74,
                0x07, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65,
                0x03, 0x63, 0x6F, 0x6D,
                0x00
            ],
            DnsRecordType.SRV => [
                0x00, 0x00, 0x00, 0x05, 0x01, 0x85,
                0x04, 0x68, 0x6F, 0x73, 0x74,
                0x07, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65,
                0x03, 0x63, 0x6F, 0x6D,
                0x00
            ],
            DnsRecordType.TXT => [0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F],
            _ => throw new ArgumentOutOfRangeException(nameof(recordType), recordType, null)
        };
        var message = request.Reply(new DnsRawRecord(name, rawData.ToArray(), recordType, DnsClass.IN, TimeSpan.FromSeconds(42)));
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize];

        var length = DnsMessageEncoder.Encode(buffer, message);
        var actualResponse = DnsResponseEncoder.Decode(buffer[..length]);

        Assert.HasCount(1, actualResponse.Answers);
        var actualAnswer = actualResponse.Answers[0];
        Assert.AreEqual(name, actualAnswer.Name);
        Assert.AreEqual(recordType, actualAnswer.RecordType);
        Assert.AreEqual(DnsClass.IN, actualAnswer.Class);
        Assert.AreEqual(TimeSpan.FromSeconds(42), actualAnswer.Ttl);

        switch (recordType)
        {
            case DnsRecordType.A:
            case DnsRecordType.AAAA:
                Assert.IsInstanceOfType<DnsAddressRecord>(actualAnswer);
                CollectionAssert.AreEqual(rawData.ToArray(), ((DnsAddressRecord)actualAnswer).Data.GetAddressBytes());
                break;
            case DnsRecordType.CNAME:
                Assert.IsInstanceOfType<DnsCNameRecord>(actualAnswer);
                Assert.AreEqual(expectedTarget, ((DnsCNameRecord)actualAnswer).Data);
                break;
            case DnsRecordType.PTR:
                Assert.IsInstanceOfType<DnsPtrRecord>(actualAnswer);
                Assert.AreEqual(expectedTarget, ((DnsPtrRecord)actualAnswer).Data);
                break;
            case DnsRecordType.SRV:
                Assert.IsInstanceOfType<DnsServiceRecord>(actualAnswer);
                Assert.AreEqual(new DnsServiceRecordData(0, 5, 389, expectedTarget), ((DnsServiceRecord)actualAnswer).Data);
                break;
            case DnsRecordType.TXT:
                Assert.IsInstanceOfType<DnsTextRecord>(actualAnswer);
                Assert.AreEqual("hello", ((DnsTextRecord)actualAnswer).Data);
                break;
        }
    }

    [TestMethod]
    public void Test_Decode_SrvData_WithTrailingBytes_Throws()
    {
        var request = new DnsRequest(DnsName.Parse("_ldap._tcp.example.com"), DnsRecordType.SRV);
        var message = request.Reply(new DnsServiceRecord(DnsName.Parse("_ldap._tcp.example.com"), 0, 5, 389, DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)));
        var buffer = new byte[DnsDefaults.MaxUdpMessageSize];

        var length = DnsMessageEncoder.Encode(buffer, message);
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
        _ = DnsNameEncoder.Decode(ref reader);
        _ = reader.Read<ushort>();
        _ = reader.Read<ushort>();
        _ = reader.Read<uint>();
        var dataLengthPosition = reader.Position;
        var dataLength = reader.Read<ushort>();
        Assert.AreEqual(length, reader.Position + dataLength);

        ++dataLength;
        buffer[dataLengthPosition] = (byte)(dataLength >> 8);
        buffer[dataLengthPosition + 1] = (byte)dataLength;
        buffer[length] = 0;

        Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(buffer.AsSpan(0, length + 1)));
    }

    [TestMethod]
    public void Test_Decode_Malformed()
    {
        Memory<byte> buffer = new byte[DnsDefaults.MaxUdpMessageSize];
        for (var i = 0; i < 1000; ++i)
        {
            var messageMem = buffer[..Random.Shared.Next(0, DnsDefaults.MaxUdpMessageSize)];
            Random.Shared.NextBytes(messageMem.Span);
            Assert.ThrowsExactly<FormatException>(() => DnsRequestEncoder.Decode(messageMem.Span));
            Assert.ThrowsExactly<FormatException>(() => DnsResponseEncoder.Decode(messageMem.Span));
        }
    }

    private static Memory<byte> CreateMessageForCompressionLoopTests(string name)
    {
        var buffer = new byte[DnsDefaults.MaxUdpMessageSize];
        var message = new DnsRequest(DnsName.Parse(name), DnsRecordType.A);
        var length = DnsMessageEncoder.Encode((Span<byte>)buffer, message);
        return buffer.AsMemory(0, length);
    }

    [TestMethod]
    public void Test_Decode_Compression_Loop()
    {
        const string testName = "test.wxyz";
        var encodedTestName = "\x0004test\x0004wxyz\0"u8;
        Assert.AreEqual(11, encodedTestName.Length); // Self-check

        var messageMem = CreateMessageForCompressionLoopTests(testName);
        var messageSpan = messageMem.Span;
        const ushort questionNameOffset = 12; // 0x0C
        var questionName = messageSpan.Slice(questionNameOffset, encodedTestName.Length);
        Assert.IsTrue(encodedTestName.SequenceEqual(questionName)); // Self-check

        // Replace "\x0004w" in "\x0004wxyz" with a pointer to the beginning of name
        const ushort wxyzLabelOffset = questionNameOffset + 5;
        messageSpan[wxyzLabelOffset] = 0xC0;  // Compression Mask
        messageSpan[wxyzLabelOffset + 1] = (byte)questionNameOffset;

        Assert.ThrowsExactly<FormatException>(() => DnsRequestEncoder.Decode(messageMem.Span)); // Former implementation was throwing StackOverflowException
    }

    [TestMethod]
    public void Test_Decode_Compression_Pointer_Refs_To_Itself()
    {
        const string testName = "test";
        var encodedTestName = "\x0004test\0"u8;
        Assert.AreEqual(6, encodedTestName.Length); // Self-check

        var messageMem = CreateMessageForCompressionLoopTests(testName);
        var messageSpan = messageMem.Span;
        const ushort questionNameOffset = 12; // 0x0C
        var questionName = messageSpan.Slice(questionNameOffset, encodedTestName.Length);
        Assert.IsTrue(encodedTestName.SequenceEqual(questionName)); // Self-check

        // Replace "\x0004t" in "\x0004test" with a pointer to itself
        messageSpan[questionNameOffset] = 0xC0;  // Compression Mask
        messageSpan[questionNameOffset + 1] = (byte)questionNameOffset;

        Assert.ThrowsExactly<FormatException>(() => DnsRequestEncoder.Decode(messageMem.Span)); // Former implementation was throwing StackOverflowException
    }

    [TestMethod]
    public void Test_Decode_Compression_Pointer_Loop()
    {
        const string testName = "test.wxyz";
        var encodedTestName = "\x0004test\x0004wxyz\0"u8;
        Assert.AreEqual(11, encodedTestName.Length); // Self-check

        var messageMem = CreateMessageForCompressionLoopTests(testName);
        var messageSpan = messageMem.Span;
        const ushort questionNameOffset = 12; // 0x0C
        var questionName = messageSpan.Slice(questionNameOffset, encodedTestName.Length);
        Assert.IsTrue(encodedTestName.SequenceEqual(questionName)); // Self-check

        // Replace "\x0004w" in "\x0004wxyz" with a pointer to the beginning of name
        const ushort wxyzLabelOffset = questionNameOffset + 5;
        messageSpan[wxyzLabelOffset] = 0xC0;  // Compression Mask
        messageSpan[wxyzLabelOffset + 1] = (byte)questionNameOffset;

        // Replace "\x0004t" in "\x0004test" with a pointer to the above pointer creating a pointer loop
        messageSpan[questionNameOffset] = 0xC0;  // Compression Mask
        messageSpan[questionNameOffset + 1] = (byte)wxyzLabelOffset;

        Assert.ThrowsExactly<FormatException>(() => DnsRequestEncoder.Decode(messageMem.Span)); // Former implementation was throwing StackOverflowException
    }

    [TestMethod]
    public void Test_Decode_MalformedMessage_OverflowIsHandled()
    {
        // This test documents that OverflowException from malformed DNS messages
        // is properly caught and wrapped in FormatException by DnsRawMessageEncoder.
        // The overflow can occur when:
        // 1. A compression pointer points beyond the buffer
        // 2. A record data length would read beyond the buffer
        // This is NOT a bug - it's proper handling of malformed/malicious DNS messages.

        var buffer = new byte[30];

        // Create a minimal DNS header
        buffer[0] = 0x00; buffer[1] = 0x01; // ID
        buffer[2] = 0x00; buffer[3] = 0x00; // Flags
        buffer[4] = 0x00; buffer[5] = 0x01; // QDCOUNT = 1
        buffer[6] = 0x00; buffer[7] = 0x00; // ANCOUNT
        buffer[8] = 0x00; buffer[9] = 0x00; // NSCOUNT
        buffer[10] = 0x00; buffer[11] = 0x00; // ARCOUNT

        // Question with compression pointer pointing beyond buffer (offset 0xFF = 255)
        buffer[12] = 0xC0; buffer[13] = 0xFF; // Compression pointer to offset 255
        buffer[14] = 0x00; buffer[15] = 0x01; // Type A
        buffer[16] = 0x00; buffer[17] = 0x01; // Class IN

        // Should throw FormatException (not OverflowException)
        var ex = Assert.ThrowsExactly<FormatException>(() => DnsRequestEncoder.Decode(buffer.AsSpan(0, 18)));

        // The inner exception should be OverflowException from GetSubReader
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }
}