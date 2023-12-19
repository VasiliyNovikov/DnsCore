﻿using System.Numerics;
using System;

using DnsCore.Encoding;

namespace DnsCore.Model;

public sealed class DnsQuestion(DnsName name, DnsRecordType recordType, DnsClass @class = DnsClass.IN)
    : DnsRecordBase(name, recordType, @class)
    , IEquatable<DnsQuestion>
    , IEqualityOperators<DnsQuestion, DnsQuestion, bool>
{
    internal new static DnsQuestion Decode(ref DnsReader reader)
    {
        var (name, type, @class) = DnsRecordBase.Decode(ref reader);
        return new DnsQuestion(name, type, @class);
    }

    public bool Equals(DnsQuestion? other) => other is not null && Class == other.Class && RecordType == other.RecordType && Name == other.Name;

    public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

    public override int GetHashCode() => HashCode.Combine(Name, RecordType, Class);

    public static bool operator ==(DnsQuestion? left, DnsQuestion? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(DnsQuestion? left, DnsQuestion? right) => !(left == right);
}