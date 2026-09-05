using System;
using System.Linq;

using DnsCore.IO;
using DnsCore.Model;
using DnsCore.Model.Encoding;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsNameTests
{
    [TestMethod]
    [DataRow("129.128/26.2.0.192.in-addr.arpa.")]
    [DataRow("john+ops.example.com.")]
    [DataRow("Office-Printer(Color)._ipp._tcp.local.")]
    [DataRow("Office-MacBook._ssh._tcp.local.")]
    public void Parse_GeneralName_RoundTrips(string text)
    {
        var name = DnsName.Parse(text);

        Assert.AreEqual(text, name.ToString());
        Assert.AreEqual(name, DnsName.Parse(text[..^1]));
    }

    [TestMethod]
    [DataRow("example.com")]
    [DataRow("3host.Example.com.")]
    [DataRow("a-b")]
    [DataRow("xn--caf-dma.example")]
    public void ParseHostName_Valid(string text)
    {
        var name = DnsName.ParseHostName(text);
        Assert.IsTrue(name.IsHostName);
        Assert.IsTrue(DnsLabel.ParseHostName(name.Label.ToString()).IsHostName);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("-a.example")]
    [DataRow("a-.example")]
    [DataRow("*.example")]
    [DataRow("_acme-challenge.example")]
    [DataRow("a/b.example")]
    [DataRow("host._srv")]
    public void HostNameValidation_RejectsNonHostNames(string text)
    {
        Assert.IsFalse(DnsName.Parse(text).IsHostName);
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text));
    }

    [TestMethod]
    [DataRow("a..b")]
    [DataRow("a..")]
    [DataRow("..")]
    [DataRow(".a")]
    [DataRow(@"a.\")]
    [DataRow(@"a\.b.example")]
    [DataRow(@"\097.example")]
    [DataRow("a b.example")]
    public void Parse_MalformedName_Throws(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text));
    }

    [TestMethod]
    public void ParseAndConstruct_EnforceLengthLimit()
    {
        var prefix = string.Join('.', Enumerable.Repeat(new string('a', 63), 3));
        var maximum = DnsName.Parse(prefix + "." + new string('b', 61));
        Assert.AreEqual(254, maximum.Length);
        var parent = DnsName.Parse(prefix);
        Assert.AreEqual(254, new DnsName(DnsLabel.Parse(new string('b', 61)), parent).Length);
        Assert.ThrowsExactly<ArgumentException>(() => new DnsName(DnsLabel.Parse(new string('b', 62)), parent));
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(prefix + "." + new string('b', 62)));
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(string.Concat(Enumerable.Repeat("a.", 10000))));
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(new string('a', 10000)));
    }

    [TestMethod]
    [DataRow(0x40)]
    [DataRow(0x80)]
    [DataRow(0xBF)]
    public void Decode_ReservedLabelType_Throws(int header)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var packet = new byte[256];
            packet[0] = (byte)header;
            var reader = new DnsReader(packet);
            DnsNameEncoder.Decode(ref reader);
        });
    }
}
