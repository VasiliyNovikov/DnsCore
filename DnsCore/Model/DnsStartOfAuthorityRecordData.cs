using System;

namespace DnsCore.Model;

public readonly record struct DnsStartOfAuthorityRecordData(DnsName PrimaryNameServer, DnsName ResponsibleMailbox, uint Serial, TimeSpan Refresh, TimeSpan Retry, TimeSpan Expire, TimeSpan Minimum)
{
    public override string ToString() => $"{PrimaryNameServer} {ResponsibleMailbox} {Serial} {(uint)Refresh.TotalSeconds} {(uint)Retry.TotalSeconds} {(uint)Expire.TotalSeconds} {(uint)Minimum.TotalSeconds}";
}