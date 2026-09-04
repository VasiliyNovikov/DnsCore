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
    public void Parse_ValidLabel_PreservesText(string text)
    {
        var label = DnsLabel.Parse(text);

        Assert.AreEqual(text, label.ToString());
        Assert.AreEqual(text.Length, label.Length);
        Assert.AreEqual(text + ".example.com.", DnsName.Parse(text + ".example.com").ToString());
    }

    [TestMethod]
    [DataRow("-*")]
    [DataRow("*-")]
    [DataRow("a/b")]
    [DataRow("a?b")]
    [DataRow("a+b")]
    [DataRow("a.b")]
    [DataRow("a\\b")]
    [DataRow("a b")]
    [DataRow("a\0b")]
    [DataRow("\u00E9")]
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
}