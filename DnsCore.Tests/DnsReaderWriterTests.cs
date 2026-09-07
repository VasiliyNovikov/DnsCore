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

    [TestMethod]
    [DataRow("Integer")]
    [DataRow("Span")]
    [DataRow("Reservation")]
    public void Test_Write_AboveMaximum_PreservesPositionAndBuffer(string operation)
    {
        var buffer = new byte[70_000];
        Array.Fill(buffer, (byte)0xA5);
        var expected = (byte[])buffer.Clone();
        var writer = new DnsWriter(buffer);
        _ = writer.ProvideBufferAndAdvance(UInt16.MaxValue - 1);

        try
        {
            switch (operation)
            {
                case "Integer": writer.Write((ushort)42); break;
                case "Span": writer.Write(new byte[2]); break;
                case "Reservation": _ = writer.ProvideBufferAndAdvance(2); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
            Assert.Fail("Expected the write to exceed the DNS message size limit.");
        }
        catch (ArgumentOutOfRangeException error)
        {
            Assert.AreEqual("length", error.ParamName);
            Assert.StartsWith($"Message exceeds the maximum length of {UInt16.MaxValue} bytes", error.Message);
        }

        Assert.AreEqual(UInt16.MaxValue - 1, writer.Position);
        Assert.AreSequenceEqual(expected, buffer);
    }

    [TestMethod]
    public void Test_Write_SpanAboveMaximum_Throws()
    {
        var data = new byte[UInt16.MaxValue + 1];

        var error = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DnsWriter(new byte[70_000]).Write(data));
        Assert.AreEqual("length", error.ParamName);
        Assert.StartsWith($"Message exceeds the maximum length of {UInt16.MaxValue} bytes", error.Message);
    }

    private static void WriteTime(TimeSpan value) => new DnsWriter(stackalloc byte[sizeof(uint)]).WriteTime(value);
}