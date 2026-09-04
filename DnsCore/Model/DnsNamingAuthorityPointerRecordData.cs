using System;

namespace DnsCore.Model;

public readonly record struct DnsNamingAuthorityPointerRecordData
{
    private static readonly System.Text.Encoding Encoding = new System.Text.UTF8Encoding(false, true);

    public ushort Order { get; }
    public ushort Preference { get; }
    public string Flags { get; }
    public string Services { get; }
    public string RegularExpression { get; }
    public DnsName Replacement { get; }

    public DnsNamingAuthorityPointerRecordData(ushort order, ushort preference, string flags, string services, string regularExpression, DnsName replacement)
    {
        ValidateString(flags, nameof(flags));
        ValidateString(services, nameof(services));
        ValidateString(regularExpression, nameof(regularExpression));
        ArgumentNullException.ThrowIfNull(replacement);

        Order = order;
        Preference = preference;
        Flags = flags;
        Services = services;
        RegularExpression = regularExpression;
        Replacement = replacement;
    }

    internal static System.Text.Encoding TextEncoding => Encoding;

    internal static void Validate(DnsNamingAuthorityPointerRecordData data)
    {
        ValidateString(data.Flags, nameof(Flags));
        ValidateString(data.Services, nameof(Services));
        ValidateString(data.RegularExpression, nameof(RegularExpression));
        ArgumentNullException.ThrowIfNull(data.Replacement);
    }

    private static void ValidateString(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (Encoding.GetByteCount(value) > Byte.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, $"The encoded value exceeds the maximum length of {Byte.MaxValue} bytes.");
    }

    public override string ToString() => $"{Order} {Preference} \"{Flags}\" \"{Services}\" \"{RegularExpression}\" {Replacement}";
}