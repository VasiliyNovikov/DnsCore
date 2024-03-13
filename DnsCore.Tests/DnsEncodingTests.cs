using System;
using System.Collections.Generic;
using System.Net;

using DnsCore.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsEncodingTests
{
   //[TestMethod]
    public void Test_Encode_Decode()
    {
        List<DnsRequest> requests = [
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.AAAA),
            new DnsRequest(DnsName.Parse("www.example.com"), DnsRecordType.CNAME),
            new DnsRequest(DnsName.Parse("unknown.example.com"), DnsRecordType.A),
            new DnsRequest(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsRecordType.PTR),
            new DnsRequest(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsRecordType.PTR)
        ];

        List<DnsResponse> responses = [
            requests[0].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("host.example.com"), IPAddress.Parse("1.2.3.4"), TimeSpan.FromSeconds(42))),
            
            requests[1].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)),
                              new DnsAddressRecord(DnsName.Parse("www.example.com"), IPAddress.Parse("::1:2:3:4"), TimeSpan.FromSeconds(42))),

            requests[2].Reply(new DnsCNameRecord(DnsName.Parse("www.example.com"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[3].Reply(DnsResponseStatus.NameError),

            requests[4].Reply(new DnsPtrRecord(DnsName.Parse("4.3.2.1.in-addr.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42))),

            requests[5].Reply(new DnsPtrRecord(DnsName.Parse("4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.ip6.arpa"), DnsName.Parse("host.example.com"), TimeSpan.FromSeconds(42)))
        ];

        List<DnsMessage> messages = [.. requests, .. responses];
        
        Span<byte> buffer = stackalloc byte[DnsDefaults.MaxUdpMessageSize]; 
        foreach (var message in messages)
        {
            var length = message.Encode(buffer);
            var messageSpan = buffer[..length];
            
            DnsMessage actualMessage = message is DnsRequest ? DnsRequest.Decode(messageSpan) : DnsResponse.Decode(messageSpan);
            
            Assert.AreEqual(message.Id, actualMessage.Id);
            Assert.AreEqual(message.RequestType, actualMessage.RequestType);
            Assert.AreEqual(message.RecursionDesired, actualMessage.RecursionDesired);
            Assert.AreEqual(message.Questions.Count, actualMessage.Questions.Count);
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
                Assert.AreEqual(response.Answers.Count, actualResponse.Answers.Count);
                for (var i = 0; i < response.Answers.Count; ++i)
                    DnsAssert.AreEqual(response.Answers[i], actualResponse.Answers[i]);
                Assert.AreEqual(response.Authorities.Count, actualResponse.Authorities.Count);
                for (var i = 0; i < response.Authorities.Count; ++i)
                    DnsAssert.AreEqual(response.Authorities[i], actualResponse.Authorities[i]);
                Assert.AreEqual(response.Additional.Count, actualResponse.Additional.Count);
                for (var i = 0; i < response.Additional.Count; ++i)
                    DnsAssert.AreEqual(response.Additional[i], actualResponse.Additional[i]);
            }

            Assert.AreEqual(length, message.Encode(buffer[..length]));

            for (var l = 0; l < length; ++l)
            {
                var smallLength = l;
                Assert.ThrowsException<FormatException>(() =>
                {
                    Span<byte> smallBuffer = stackalloc byte[smallLength]; 
                    message.Encode(smallBuffer);
                });

                Assert.ThrowsException<FormatException>(() =>
                {
                    Span<byte> buffer = stackalloc byte[length];
                    message.Encode(buffer);

                    var smallBuffer = buffer[..smallLength];
                    if (message is DnsRequest)
                        DnsRequest.Decode(smallBuffer);
                    else
                        DnsResponse.Decode(smallBuffer);
                });
            }
        }
    }

    [TestMethod]
    public void Test_Decode_Malformed()
    {
        Memory<byte> buffer = new byte[DnsDefaults.MaxUdpMessageSize];
        for (var i = 0; i < 1000; ++i)
        {
            var messageMem = buffer[..Random.Shared.Next(0, DnsDefaults.MaxUdpMessageSize)];
            Random.Shared.NextBytes(messageMem.Span);
            Assert.ThrowsException<FormatException>(() => DnsRequest.Decode(messageMem.Span));
            Assert.ThrowsException<FormatException>(() => DnsResponse.Decode(messageMem.Span));
        }
    }
}