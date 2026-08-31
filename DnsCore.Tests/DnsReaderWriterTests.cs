using System;

using DnsCore.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsCore.Tests;

[TestClass]
public class DnsReaderWriterTests
{
    [TestMethod]
    [DataRow(0u)]
    [DataRow(42u)]
    [DataRow(uint.MaxValue)]
    public void Test_WriteTime_ReadTime_RoundTrips(uint seconds)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        var writer = new DnsWriter(buffer);
        var expected = TimeSpan.FromSeconds(seconds);

        writer.WriteTime(expected);
        var reader = new DnsReader(buffer);

        Assert.AreEqual(expected, reader.ReadTime());
        Assert.AreEqual(sizeof(uint), writer.Position);
        Assert.AreEqual(sizeof(uint), reader.Position);
    }

    [TestMethod]
    public void Test_WriteTime_NegativeValue_Throws()
    {
        Assert.ThrowsExactly<OverflowException>(() => WriteTime(TimeSpan.FromSeconds(-1)));
    }

    [TestMethod]
    public void Test_WriteTime_ValueGreaterThanUIntMax_Throws()
    {
        Assert.ThrowsExactly<OverflowException>(() => WriteTime(TimeSpan.FromSeconds((double)uint.MaxValue + 1)));
    }

    private static void WriteTime(TimeSpan value) => new DnsWriter(stackalloc byte[sizeof(uint)]).WriteTime(value);
}