using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsCore.Model;

public sealed class DnsCNameRecord(DnsName name, DnsName alias, TimeSpan ttl)
    : DnsNameRecord(name, DnsRecordType.CNAME, alias, ttl)
{
    public DnsName Alias => AnswerName;
}