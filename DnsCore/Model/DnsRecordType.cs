namespace DnsCore.Model;

public enum DnsRecordType : ushort
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
    // Responsible person (EXPERIMENTAL)
    RP = 17,
    // AFS database location (EXPERIMENTAL; subtype 1 is deprecated in favor of SRV)
    AFSDB = 18,
    // Route through (EXPERIMENTAL)
    RT = 21,
    // Legacy security signature, retained for SIG(0)
    SIG = 24,
    // Legacy public key, retained for SIG(0) and TKEY
    KEY = 25,
    // Legacy MIXER/X.400 mail mapping information
    PX = 26,
    // An IPv6 host address
    AAAA = 28,
    // Next domain (OBSOLETE)
    NXT = 30,
    // Server selection
    SRV = 33,
    // Naming authority pointer
    NAPTR = 35,
    // Non-terminal DNS name redirection
    DNAME = 39,
    // A request for a transfer of an entire zone
    AXFR = 252,
    // A request for mailbox-related records (MB, MG or MR)
    MAILB = 253,
    // A request for mail agent RRs (Obsolete - see MX)
    MAILA = 254,
    // A request for all records
    ALL = 255
}