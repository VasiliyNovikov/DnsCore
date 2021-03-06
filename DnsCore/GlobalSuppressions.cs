﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Scope = "type", Target = "~T:DnsCore.DnsClass")]
[assembly: SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Scope = "type", Target = "~T:DnsCore.DnsType")]

[assembly: SuppressMessage("Naming", "CA1720:Identifier contains type name", Scope = "type", Target = "~T:DnsCore.DnsType")]

[assembly: SuppressMessage("Style", "IDE0056:Use index operator", Scope = "member", Target = "~M:DnsCore.DnsLabel.#ctor(System.String)")]
