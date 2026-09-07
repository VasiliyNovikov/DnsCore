using System;

namespace DnsCore.Model;

public readonly record struct DnsKeyRecordData
{
    private const ushort NoKeyMask = 0xC000;

    public ushort Flags { get; }
    public byte Protocol { get; }
    public byte Algorithm { get; }
    public DnsName? PrivateAlgorithmName { get; }
    public byte[] PublicKey { get; }

    public DnsKeyRecordData(ushort flags, byte protocol, byte algorithm, DnsName? privateAlgorithmName, byte[] publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        ValidateAlgorithm(flags, algorithm, privateAlgorithmName, publicKey);

        Flags = flags;
        Protocol = protocol;
        Algorithm = algorithm;
        PrivateAlgorithmName = privateAlgorithmName;
        PublicKey = publicKey;
    }

    internal static void Validate(DnsKeyRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.PublicKey);
        ValidateAlgorithm(data.Flags, data.Algorithm, data.PrivateAlgorithmName, data.PublicKey);
    }

    private static void ValidateAlgorithm(ushort flags, byte algorithm, DnsName? privateAlgorithmName, byte[] publicKey)
    {
        if ((flags & NoKeyMask) == NoKeyMask)
        {
            if (privateAlgorithmName is not null || publicKey.Length != 0)
                throw new ArgumentException("KEY records that declare no key cannot contain key data", nameof(publicKey));
            return;
        }

        if (algorithm == 253 && privateAlgorithmName is null)
            throw new ArgumentException("KEY private algorithm name is required for algorithm 253 unless the record declares that no key is present", nameof(privateAlgorithmName));
        if (algorithm != 253 && privateAlgorithmName is not null)
            throw new ArgumentException("KEY private algorithm name is only valid for algorithm 253", nameof(privateAlgorithmName));
    }

    public override string ToString()
    {
        var privateAlgorithm = PrivateAlgorithmName is null ? "" : $" {PrivateAlgorithmName}";
        return $"{Flags} {Protocol} {Algorithm}{privateAlgorithm} {Convert.ToBase64String(PublicKey)}";
    }
}