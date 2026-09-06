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
    [DataRow("")]
    [DataRow(".")]
    [DataRow("Example")]
    [DataRow("Example.com")]
    [DataRow("Example.com.")]
    [DataRow("129.128/26.2.0.192.in-addr.arpa.")]
    [DataRow("john+ops.example.com.")]
    [DataRow("Office-Printer(Color)._ipp._tcp.local.")]
    [DataRow("Office-MacBook._ssh._tcp.local.")]
    [DataRow("*.example.com")]
    public void ParseAndTryParse_ValidName_RoundTrips(string text)
    {
        var name = DnsName.Parse(text);
        var expected = text.EndsWith('.') ? text : text + ".";
        Assert.AreEqual(expected, name.ToString());
        Assert.IsTrue(DnsName.TryParse(text, out var parsed));
        Assert.AreEqual(name, parsed);
        Assert.AreEqual(expected, parsed.ToString());
        Assert.AreEqual(name, DnsName.Parse(expected[..^1]));
        Assert.IsTrue(DnsName.TryParse(expected[..^1], out parsed));
        Assert.AreEqual(name, parsed);
        if (text is "" or ".")
        {
            Assert.AreSame(DnsName.Empty, name);
            Assert.AreSame(DnsName.Empty, parsed);
        }
    }

    [TestMethod]
    [DataRow("..")]
    [DataRow("a...")]
    [DataRow(@"a.\")]
    [DataRow(@"a\.b.example")]
    [DataRow(@"\097.example")]
    [DataRow("a b.example")]
    [DataRow("a.b\\c")]
    [DataRow("a.b\0c")]
    [DataRow("a.b\tc")]
    [DataRow("a.b\nc")]
    [DataRow("a.b\u007Fc")]
    [DataRow("a.caf\u00E9")]
    [DataRow("a.\U0001F600")]
    public void ParseAndTryParse_InvalidName(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text));
        DnsName? name = DnsName.Parse("previous");
        Assert.IsFalse(DnsName.TryParse(text, out name));
        Assert.IsNull(name);
        Assert.IsFalse(DnsName.TryParseHostName(text, out name));
        Assert.IsNull(name);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(".")]
    public void ParseAndTryParse_EnforceLengthLimits(string trailingDot)
    {
        var maximum = string.Join('.', Enumerable.Repeat(new string('a', 63), 3)) + "." + new string('b', 61);
        Assert.IsTrue(DnsName.TryParse(maximum + trailingDot, out var name));
        Assert.IsNotNull(name);
        Assert.AreEqual(254, name.Length);
        Assert.AreEqual(name, DnsName.Parse(maximum + trailingDot));

        foreach (var text in new[] { maximum + "b" + trailingDot, "a." + new string('b', 64) + trailingDot, string.Concat(Enumerable.Repeat("a.", 10000)), new string('a', 10000) })
        {
            Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text));
            Assert.IsFalse(DnsName.TryParse(text, out name));
            Assert.IsNull(name);
        }
        Assert.AreEqual("Name length exceeds maximum length", Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(maximum + "b" + trailingDot)).Message);
        Assert.AreEqual("Label length exceeds maximum length", Assert.ThrowsExactly<FormatException>(() => DnsName.Parse("a." + new string('b', 64) + trailingDot)).Message);
    }

    [TestMethod]
    public void ParseAndTryParse_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DnsName.Parse(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => DnsName.ParseHostName(null));
        DnsName? name = DnsName.Parse("previous");
        Assert.IsFalse(DnsName.TryParse(null, out name));
        Assert.IsNull(name);
        Assert.IsFalse(DnsName.TryParseHostName(null, out name));
        Assert.IsNull(name);
    }

    [TestMethod]
    [DataRow(".a", "DNS name contains an empty label")]
    [DataRow("a..b", "DNS name contains an empty label")]
    [DataRow("a..", "DNS name contains an empty label")]
    [DataRow("a.b c", "DNS labels require printable ASCII without spaces, dots, or backslashes")]
    public void ParseAndTryParse_InvalidName_ReportsValidationError(string text, string error)
    {
        Assert.AreEqual(error, Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text)).Message);
        Assert.AreEqual(error, Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text)).Message);
        DnsName? name = DnsName.Parse("previous");
        Assert.IsFalse(DnsName.TryParse(text, out name));
        Assert.IsNull(name);
        Assert.IsFalse(DnsName.TryParseHostName(text, out name));
        Assert.IsNull(name);
    }

    [TestMethod]
    [DataRow("example.com")]
    [DataRow("3host.Example.com.")]
    [DataRow("a-b")]
    [DataRow("xn--caf-dma.example")]
    public void ParseAndTryParseHostName_Valid(string text)
    {
        var name = DnsName.ParseHostName(text);
        Assert.IsTrue(name.IsHostName);
        Assert.IsTrue(DnsLabel.ParseHostName(name.Label.ToString()).IsHostName);
        Assert.IsTrue(DnsLabel.TryParseHostName(name.Label.ToString(), out var label));
        Assert.AreEqual(name.Label, label);
        Assert.IsTrue(DnsName.TryParseHostName(text, out var parsed));
        Assert.AreEqual(name, parsed);
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
    public void ParseAndTryParseHostName_RejectNonHostNames(string text)
    {
        Assert.IsFalse(DnsName.Parse(text).IsHostName);
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text));
        Assert.IsFalse(DnsName.TryParseHostName(text, out var name));
        Assert.AreEqual(DnsName.Parse(text), name);
    }

    [TestMethod]
    public void Construct_EnforcesLengthLimit()
    {
        var prefix = string.Join('.', Enumerable.Repeat(new string('a', 63), 3));
        var parent = DnsName.Parse(prefix);
        Assert.AreEqual(254, new DnsName(DnsLabel.Parse(new string('b', 61)), parent).Length);
        Assert.ThrowsExactly<ArgumentException>(() => new DnsName(DnsLabel.Parse(new string('b', 62)), parent));
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
