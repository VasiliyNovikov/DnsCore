namespace DnsCore.Model;

public enum DnsClass
{
    // The Internet
    IN = 1,
    // The CSNET class (Obsolete - used only for examples in some obsolete RFCs)
    CS = 2,
    // The CHAOS class
    CH = 3,
    // Hesiod [Dyer 87]
    HS = 4,
    // Any class
    ANY = 255
}