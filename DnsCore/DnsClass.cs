namespace DnsCore
{
    public enum DnsClass : byte
    {
        /// <summary>
        /// The Internet
        /// </summary>
        IN = 1,
        /// <summary>
        /// The CSNET class (Obsolete - used only for examples in some obsolete RFCs)
        /// </summary>
        CS = 2,
        /// <summary>
        /// The CHAOS class
        /// </summary>
        CH = 3,
        /// <summary>
        /// Hesiod [Dyer 87]
        /// </summary>
        HS = 4,

        /// <summary>
        /// Any class
        /// </summary>
        ANY = 255
    }
}