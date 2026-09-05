using System;

using DnsCore.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsLabelTests
{
    [TestMethod]
    [DataRow("*")]
    [DataRow("**")]
    [DataRow("*a")]
    [DataRow("a*")]
    [DataRow("a*b")]
    [DataRow("*-*")]
    [DataRow("_srv")]
    [DataRow("a-b")]
    [DataRow("Az09")]
    [DataRow("-*")]
    [DataRow("*-")]
    [DataRow("a/b")]
    [DataRow("a?b")]
    [DataRow("a+b")]
    [DataRow("!")]
    [DataRow("~")]
    public void Parse_ValidLabel_PreservesText(string text)
    {
        var label = DnsLabel.Parse(text);

        Assert.AreEqual(text, label.ToString());
        Assert.AreEqual(text.Length, label.Length);
        Assert.AreEqual(text + ".example.com.", DnsName.Parse(text + ".example.com").ToString());
    }

    [TestMethod]
    [DataRow("a\\")]
    [DataRow(@"\097")]
    [DataRow(@"a\.b")]
    [DataRow("a.b")]
    [DataRow("a b")]
    [DataRow("a\0b")]
    [DataRow("a\tb")]
    [DataRow("a\nb")]
    [DataRow("a\u007Fb")]
    public void Parse_InvalidLabel_Throws(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text));
    }

    [TestMethod]
    public void Parse_AsteriskLabel_EnforcesLengthLimit()
    {
        var text = new string('*', 63);

        Assert.AreEqual(text, DnsLabel.Parse(text).ToString());
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text + "*"));
    }

    [TestMethod]
    public void Parse_AsteriskName_IsLiteral()
    {
        var name = DnsName.Parse("*.a*b.**.example.com.");

        Assert.AreEqual("*.a*b.**.example.com.", name.ToString());
        Assert.AreEqual(name, DnsName.Parse("*.a*b.**.example.com"));
        Assert.AreNotEqual(DnsName.Parse("*.example.com"), DnsName.Parse("host.example.com"));
        Assert.AreEqual(DnsLabel.Empty, DnsLabel.Parse(""));
        Assert.AreEqual(DnsName.Empty, DnsName.Parse("."));
    }

    [TestMethod]
    [DataRow("john+ops")]
    [DataRow("128/26")]
    [DataRow("Office-Printer(Color)")]
    public void Parse_Presentation_RoundTrips(string text)
    {
        var label = DnsLabel.Parse(text);
        Assert.AreEqual(text, new string(label.Span));
        var buffer = new char[text.Length];
        Assert.IsTrue(label.TryFormat(buffer, out var written, default, null));
        Assert.AreEqual(text.Length, written);
        Assert.AreEqual(text, new string(buffer));
        Assert.IsFalse(label.TryFormat(buffer.AsSpan(1), out written, default, null));
        Assert.AreEqual(0, written);
    }

    [TestMethod]
    [DataRow("-a")]
    [DataRow("a-")]
    [DataRow("*")]
    [DataRow("_srv")]
    [DataRow("a/b")]
    public void ParseHostName_RejectsNonHostLabel(string text)
    {
        Assert.IsFalse(DnsLabel.Parse(text).IsHostName);
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.ParseHostName(text));
    }

    [TestMethod]
    [DataRow("\u0080")]
    [DataRow("caf\u00E9")]
    [DataRow("\\\u00E9")]
    [DataRow("\U0001F600")]
    public void Parse_NonAsciiText_Throws(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text + ".example"));
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.ParseHostName(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text + ".example"));
    }

    [TestMethod]
    public void Equality_FoldsAsciiOnly_AndHashesConsistently()
    {
        var lower = DnsLabel.Parse("abc");
        var upper = DnsLabel.Parse("ABC");
        Assert.AreEqual(lower, upper);
        Assert.AreEqual(lower.GetHashCode(), upper.GetHashCode());
    }
}
