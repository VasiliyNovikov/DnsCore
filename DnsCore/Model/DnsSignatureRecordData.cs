using System;

namespace DnsCore.Model;

public readonly record struct DnsSignatureRecordData
{
    public DnsRecordType TypeCovered { get; }
    public byte Algorithm { get; }
    public byte Labels { get; }
    public TimeSpan OriginalTtl { get; }
    public uint SignatureExpiration { get; }
    public uint SignatureInception { get; }
    public ushort KeyTag { get; }
    public DnsName SignerName { get; }
    public DnsName? PrivateAlgorithmName { get; }
    public byte[] Signature { get; }

    public DnsSignatureRecordData(DnsRecordType typeCovered, byte algorithm, byte labels, TimeSpan originalTtl, uint signatureExpiration, uint signatureInception, ushort keyTag, DnsName signerName, DnsName? privateAlgorithmName, byte[] signature)
    {
        ValidateOriginalTtl(originalTtl);
        ArgumentNullException.ThrowIfNull(signerName);
        ArgumentNullException.ThrowIfNull(signature);
        ValidateAlgorithm(algorithm, privateAlgorithmName, signature);

        TypeCovered = typeCovered;
        Algorithm = algorithm;
        Labels = labels;
        OriginalTtl = originalTtl;
        SignatureExpiration = signatureExpiration;
        SignatureInception = signatureInception;
        KeyTag = keyTag;
        SignerName = signerName;
        PrivateAlgorithmName = privateAlgorithmName;
        Signature = signature;
    }

    internal static void ValidateOriginalTtl(TimeSpan value)
    {
        if (value < TimeSpan.Zero || value.TotalSeconds > UInt32.MaxValue || value.TotalSeconds != Math.Truncate(value.TotalSeconds))
            throw new ArgumentOutOfRangeException(nameof(value), "The original TTL must be a whole number of seconds in the unsigned 32-bit range.");
    }

    internal static void Validate(DnsSignatureRecordData data)
    {
        ValidateOriginalTtl(data.OriginalTtl);
        ArgumentNullException.ThrowIfNull(data.SignerName);
        ArgumentNullException.ThrowIfNull(data.Signature);
        ValidateAlgorithm(data.Algorithm, data.PrivateAlgorithmName, data.Signature);
    }

    private static void ValidateAlgorithm(byte algorithm, DnsName? privateAlgorithmName, byte[] signature)
    {
        if ((algorithm == 253) != (privateAlgorithmName is not null))
            throw new ArgumentException("SIG private algorithm name must be provided only for algorithm 253", nameof(privateAlgorithmName));
        if (algorithm != 253 && signature.Length == 0)
            throw new ArgumentException("SIG signature cannot be empty", nameof(signature));
    }

    public override string ToString()
    {
        var privateAlgorithm = PrivateAlgorithmName is null ? "" : $" {PrivateAlgorithmName}";
        return $"{TypeCovered} {Algorithm} {Labels} {(uint)OriginalTtl.TotalSeconds} {SignatureExpiration} {SignatureInception} {KeyTag} {SignerName}{privateAlgorithm} {Convert.ToBase64String(Signature)}";
    }
}