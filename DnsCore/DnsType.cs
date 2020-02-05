namespace DnsCore
{
    public enum DnsType : byte
    {
        /// <summary>
        /// A host address
        /// </summary>
        A = 1,
        /// <summary>
        /// An authoritative name server
        /// </summary>
        NS = 2,
        /// <summary>
        /// A mail destination (Obsolete - use MX)
        /// </summary>
        MD = 3,
        /// <summary>
        /// A mail forwarder (Obsolete - use MX)
        /// </summary>
        MF = 4,
        /// <summary>
        /// The canonical name for an alias
        /// </summary>
        CNAME = 5,
        /// <summary>
        /// Marks the start of a zone of authority
        /// </summary>
        SOA = 6,
        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL)
        /// </summary>
        MB = 7,
        /// <summary>
        /// A mail group member (EXPERIMENTAL)
        /// </summary>
        MG = 8,
        /// <summary>
        /// A mail rename domain name (EXPERIMENTAL)
        /// </summary>
        MR = 9,
        /// <summary>
        /// A null RR (EXPERIMENTAL)
        /// </summary>
        NULL = 10,
        /// <summary>
        /// A well known service description
        /// </summary>
        WKS = 11,
        /// <summary>
        /// A domain name pointer
        /// </summary>
        PTR = 12,
        /// <summary>
        /// Host information
        /// </summary>
        HINFO = 13,
        /// <summary>
        /// Mailbox or mail list information
        /// </summary>
        MINFO = 14,
        /// <summary>
        /// Mail exchange
        /// </summary>
        MX = 15,
        /// <summary>
        /// Text strings
        /// </summary>
        TXT = 16,

        /// <summary>
        /// A request for a transfer of an entire zone
        /// </summary>
        AXFR = 252,
        /// <summary>
        /// A request for mailbox-related records (MB, MG or MR)
        /// </summary>
        MAILB = 253,
        /// <summary>
        /// A request for mail agent RRs (Obsolete - see MX)
        /// </summary>
        MAILA = 254,
        /// <summary>
        /// A request for all records
        /// </summary>
        ALL = 255
    }
}