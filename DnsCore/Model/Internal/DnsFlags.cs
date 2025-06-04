using System;
using System.Diagnostics.CodeAnalysis;

namespace DnsCore.Model.Internal;

[SuppressMessage("Microsoft.Design", "CA1069: Enums values should not be duplicated", Justification = "By design")]
[Flags]
internal enum DnsFlags : ushort
{
    // Query/Response Flag (QR) - 1 bit
    Response             = 0b_1000_0000_0000_0000,
    Query                = 0b_0000_0000_0000_0000,

    // Query Type (OPCODE) - bits 11-14 (4 bits)
    OpCodeMask           = 0b_0111_1000_0000_0000,
    OpCodeQuery          = 0b_0000_0000_0000_0000, // Standard query (0)
    OpCodeIQuery         = 0b_0000_1000_0000_0000, // Inverse query (1)
    OpCodeStatus         = 0b_0001_0000_0000_0000, // Server status request (2)
    // 3-15 Reserved for future use

    // Authoritative Answer Flag (AA) - 1 bit
    AuthoritativeAnswer  = 0b_0000_0100_0000_0000,

    // Truncation Flag (TC) - 1 bit
    Truncated            = 0b_0000_0010_0000_0000,

    // Recursion Desired (RD) - 1 bit
    RecursionDesired     = 0b_0000_0001_0000_0000,

    // Recursion Available (RA) - 1 bit
    RecursionAvailable   = 0b_0000_0000_1000_0000,

    // Reserved (for future use) - 3 bits

    // Response Code (RCODE) - 4 bits
    ResponseCodeMask     = 0b_0000_0000_0000_1111,
    NoError              = 0b_0000_0000_0000_0000,
    FormatError          = 0b_0000_0000_0000_0001,
    ServerFailure        = 0b_0000_0000_0000_0010,
    NameError            = 0b_0000_0000_0000_0011,
    NotImplemented       = 0b_0000_0000_0000_0100,
    Refused              = 0b_0000_0000_0000_0101,
    // 6-15 Reserved for future use
}