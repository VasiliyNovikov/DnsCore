using System;

using DnsCore.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsLabelTests
{
    [TestMethod]
    public void ParseAndTryParse_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DnsLabel.Parse(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => DnsLabel.ParseHostName(null));
        var label = DnsLabel.Parse("previous");
        Assert.IsFalse(DnsLabel.TryParse(null, out label));
        Assert.AreEqual(default, label);
        Assert.IsFalse(DnsLabel.TryParseHostName(null, out label));
        Assert.AreEqual(default, label);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("john+ops")]
    [DataRow("128/26")]
    [DataRow("Office-Printer(Color)")]
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
    public void ParseAndTryParse_ValidLabel_PreservesText(string text)
    {
        var label = DnsLabel.Parse(text);

        Assert.AreEqual(text, label.ToString());
        Assert.AreEqual(text.Length, label.Length);
        Assert.IsTrue(DnsLabel.TryParse(text, out var parsed));
        Assert.AreEqual(label, parsed);
        Assert.AreEqual(text, parsed.ToString());
        if (text.Length > 0)
            Assert.AreEqual(text + ".example.com.", DnsName.Parse(text + ".example.com").ToString());
    }

    [TestMethod]
    [DataRow("a\\")]
    [DataRow(".")]
    [DataRow("a\\b")]
    [DataRow(@"\097")]
    [DataRow(@"a\.b")]
    [DataRow("a.b")]
    [DataRow("a b")]
    [DataRow("a\0b")]
    [DataRow("a\tb")]
    [DataRow("a\nb")]
    [DataRow("a\u007Fb")]
    public void ParseAndTryParse_InvalidLabel(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text));
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.ParseHostName(text));
        var label = DnsLabel.Parse("previous");
        Assert.IsFalse(DnsLabel.TryParse(text, out label));
        Assert.AreEqual(default, label);
        Assert.IsFalse(DnsLabel.TryParseHostName(text, out label));
        Assert.AreEqual(default, label);
    }

    [TestMethod]
    [DataRow('a')]
    [DataRow('*')]
    public void ParseAndTryParse_EnforceLengthLimit(char character)
    {
        var text = new string(character, 63);

        Assert.AreEqual(text, DnsLabel.Parse(text).ToString());
        Assert.IsTrue(DnsLabel.TryParse(text, out var label));
        Assert.AreEqual(text, label.ToString());
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text + character));
        Assert.IsFalse(DnsLabel.TryParse(text + character, out label));
        Assert.AreEqual(default, label);
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
    public void ParseAndTryParseHostName_RejectNonHostLabel(string text)
    {
        Assert.IsFalse(DnsLabel.Parse(text).IsHostName);
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.ParseHostName(text));
        Assert.IsFalse(DnsLabel.TryParseHostName(text, out var label));
        Assert.AreEqual(DnsLabel.Parse(text), label);
    }

    [TestMethod]
    [DataRow("\u0080")]
    [DataRow("caf\u00E9")]
    [DataRow("\\\u00E9")]
    [DataRow("\U0001F600")]
    public void ParseAndTryParse_NonAsciiText(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.Parse(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.Parse(text + ".example"));
        Assert.ThrowsExactly<FormatException>(() => DnsLabel.ParseHostName(text));
        Assert.ThrowsExactly<FormatException>(() => DnsName.ParseHostName(text + ".example"));
        Assert.IsFalse(DnsLabel.TryParse(text, out var label));
        Assert.AreEqual(default, label);
        Assert.IsFalse(DnsName.TryParse(text + ".example", out var name));
        Assert.IsNull(name);
        Assert.IsFalse(DnsLabel.TryParseHostName(text, out label));
        Assert.AreEqual(default, label);
        Assert.IsFalse(DnsName.TryParseHostName(text + ".example", out name));
        Assert.IsNull(name);
    }

    [TestMethod]
    public void Equality_FoldsAsciiOnly_AndHashesConsistently()
    {
        var lower = DnsLabel.Parse("abc");
        var upper = DnsLabel.Parse("ABC");
        Assert.AreEqual(lower, upper);
        Assert.AreEqual(lower.GetHashCode(), upper.GetHashCode());
    }

    [TestMethod]
    public void Equality_DefaultAndEmpty_AndHashesConsistently()
    {
        var label = default(DnsLabel);
        Assert.AreEqual(DnsLabel.Empty, label);
        Assert.AreEqual(DnsLabel.Empty.GetHashCode(), label.GetHashCode());

        var name = new DnsName(label, null);
        Assert.AreEqual(DnsName.Empty, name);
        Assert.AreEqual(DnsName.Empty.GetHashCode(), name.GetHashCode());
    }
}
