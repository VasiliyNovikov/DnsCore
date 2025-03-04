using System;

namespace DnsCore.Model;

public abstract class DnsRecord(DnsName name, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecordBase(name, recordType, @class)
{
    public TimeSpan Ttl => ttl;

    public string Data => DataToString();

    private protected abstract string DataToString();

    public override string ToString() => $"{base.ToString()} {(uint)ttl.TotalSeconds,5} {Data}";
}

public abstract class DnsRecord<T>(DnsName name, T data, DnsRecordType recordType, DnsClass @class, TimeSpan ttl)
    : DnsRecord(name, recordType, @class, ttl) where T : notnull
{
    public new T Data => data;
    private protected override string DataToString() => data.ToString()!;
}