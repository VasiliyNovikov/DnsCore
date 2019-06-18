namespace DnsCore.Model;

public enum DnsRecordType
{
    // A host address
    A = 1,
    // An authoritative name server
    NS = 2,
    // A mail destination (Obsolete - use MX)
    MD = 3,
    // A mail forwarder (Obsolete - use MX)
    MF = 4,
    // The canonical name for an alias
    CNAME = 5,
    // Marks the start of a zone of authority
    SOA = 6,
    // A mailbox domain name (EXPERIMENTAL)
    MB = 7,
    // A mail group member (EXPERIMENTAL)
    MG = 8,
    // A mail rename domain name (EXPERIMENTAL)
    MR = 9,
    // A null RR (EXPERIMENTAL)
    NULL = 10,
    // A well known service description
    WKS = 11,
    // A domain name pointer
    PTR = 12,
    // Host information
    HINFO = 13,
    // Mailbox or mail list information
    MINFO = 14,
    // Mail exchange
    MX = 15,
    // Text strings
    TXT = 16,
    // An IPv6 host address
    AAAA = 28,
    // A request for a transfer of an entire zone
    AXFR = 252,
    // A request for mailbox-related records (MB, MG or MR)
    MAILB = 253,
    // A request for mail agent RRs (Obsolete - see MX)
    MAILA = 254,
    // A request for all records
    ALL = 255
}