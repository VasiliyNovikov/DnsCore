using System;

namespace DnsCore.Model;

public readonly record struct DnsNextDomainRecordData
{
    public DnsName NextDomainName { get; }
    public byte[] TypeBitmap { get; }

    public DnsNextDomainRecordData(DnsName nextDomainName, byte[] typeBitmap)
    {
        Validate(nextDomainName, typeBitmap);
        NextDomainName = nextDomainName;
        TypeBitmap = typeBitmap;
    }

    internal static void Validate(DnsNextDomainRecordData data) => Validate(data.NextDomainName, data.TypeBitmap);

    private static void Validate(DnsName nextDomainName, byte[] typeBitmap)
    {
        ArgumentNullException.ThrowIfNull(nextDomainName);
        ArgumentNullException.ThrowIfNull(typeBitmap);
        if (typeBitmap.Length == 0)
            throw new ArgumentException("NXT record type bitmap cannot be empty", nameof(typeBitmap));
    }

    public override string ToString() => $"{NextDomainName} {Convert.ToHexString(TypeBitmap)}";
}