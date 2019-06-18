using System.Numerics;
using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public class DnsQuestion(DnsName name, DnsRecordType recordType, DnsClass @class = DnsClass.IN)
    : IEquatable<DnsQuestion>, IEqualityOperators<DnsQuestion, DnsQuestion, bool>
{
    public DnsName Name { get; } = name;
    public DnsRecordType RecordType { get; } = recordType;
    public DnsClass Class { get; } = @class;

    public override string ToString() => $"{Name,-16} {Class,-4} {RecordType,-6}";

    internal virtual void Encode(ref DnsWriter writer)
    {
        Name.Encode(ref writer);
        writer.Write((ushort)RecordType);
        writer.Write((ushort)Class);
    }

    internal static DnsQuestion Decode(ref DnsReader reader)
    {
        var name = DnsName.Decode(ref reader);
        var type = (DnsRecordType)reader.Read<ushort>();
        var @class = (DnsClass)reader.Read<ushort>();
        return new DnsQuestion(name, type, @class);
    }

    public bool Equals(DnsQuestion? other) => other is not null && Class == other.Class && RecordType == other.RecordType && Name == other.Name;

    public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

    public override int GetHashCode() => HashCode.Combine(Name, RecordType, Class);

    public static bool operator ==(DnsQuestion? left, DnsQuestion? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(DnsQuestion? left, DnsQuestion? right) => !(left == right);
}