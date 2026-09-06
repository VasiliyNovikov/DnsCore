using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DnsCore.Model;

public sealed class DnsName
    : IEquatable<DnsName>
    , IEqualityOperators<DnsName, DnsName, bool>
    , ISpanFormattable
{
    /// <summary>Maximum presentation length, including the trailing dot.</summary>
    public const byte MaxLength = 254;
    private const char Separator = '.';

    public static DnsName Empty { get; } = new(DnsLabel.Empty, null);

    public int Length { get; }

    public bool IsEmpty => Length == 1;

    public bool IsHostName => !IsEmpty && Label.IsHostName && (Parent is null || Parent.IsEmpty || Parent.IsHostName);

    public DnsLabel Label { get; }

    public DnsName? Parent { get; }

    public DnsName(DnsLabel label, DnsName? parent)
    {
        if (label.IsEmpty && parent is not null)
            throw new ArgumentException("Parent name should be null if name label is empty", nameof(parent));

        var length = label.Length + 1;
        if (parent is not null && !parent.IsEmpty)
        {
            length += parent.Length;
            if (length > MaxLength)
                throw new ArgumentException("Name length exceeds maximum length", nameof(parent));
        }

        Label = label;
        Parent = parent;
        Length = length;
    }

    private static bool TryParseCore(ReadOnlyMemory<char> name, [NotNullWhen(true)] out DnsName? result, [NotNullWhen(false)] out string? validationError)
    {
        result = null;
        validationError = null;
        if (!name.IsEmpty && name.Span[^1] == Separator)
            name = name[..^1];

        if (name.Length == 0)
        {
            result = Empty;
            return true;
        }
        if (name.Length > MaxLength - 1)
        {
            validationError = "Name length exceeds maximum length";
            return false;
        }
        if (name.Span[^1] == Separator)
        {
            validationError = "DNS name contains an empty label";
            return false;
        }

        var separatorIndex = name.Span.IndexOf(Separator);
        var labelText = separatorIndex == -1 ? name : name[..separatorIndex];
        if (labelText.IsEmpty)
        {
            validationError = "DNS name contains an empty label";
            return false;
        }
        if (!DnsLabel.TryParseCore(labelText, out var label, out validationError))
            return false;

        var parent = Empty;
        if (separatorIndex != -1 && !TryParseCore(name[(separatorIndex + 1)..], out parent, out validationError))
            return false;

        result = new DnsName(label, parent);
        return true;
    }

    public static DnsName Parse(string? name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return TryParseCore(name.AsMemory(), out var result, out var error) ? result : throw new FormatException(error);
    }

    /// <summary>Tries to parse literal ASCII DNS text, optionally ending in a dot. Null or invalid input returns false and null.</summary>
    public static bool TryParse(string? name, [NotNullWhen(true)] out DnsName? result)
    {
        result = null;
        return name is not null && TryParseCore(name.AsMemory(), out result, out _);
    }

    /// <summary>Parses literal ASCII hostname text, optionally ending in a dot. Does not perform IDNA conversion.</summary>
    public static DnsName ParseHostName(string? name)
    {
        return Parse(name) is { IsHostName: true } result
            ? result
            : throw new FormatException("Invalid hostname");
    }

    public static bool TryParseHostName(string? name, [NotNullWhen(true)] out DnsName? result) => TryParse(name, out result) && result.IsHostName;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (!Label.TryFormat(destination, out charsWritten, format, provider))
            return false;

        if (charsWritten >= destination.Length)
            return false;

        destination[charsWritten++] = Separator;

        if (Parent is null || Parent.IsEmpty)
            return true;

        if (Parent.TryFormat(destination[charsWritten..], out var parentCharsWritten, format, provider))
        {
            charsWritten += parentCharsWritten;
            return true;
        }

        return false;
    }

    [SkipLocalsInit]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> buffer = stackalloc char[Length];
        TryFormat(buffer, out _, default, formatProvider);
        return new string(buffer);
    }

    public override string ToString() => ToString(null, null);

    public bool Equals(DnsName? other)
    {
        return other is not null &&
               Length == other.Length &&
               Label == other.Label &&
               (Parent?.Equals(other.Parent) ?? other.Parent is null);
    }

    public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

    public override int GetHashCode() => HashCode.Combine(Label, Parent);

    public static bool operator ==(DnsName? left, DnsName? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(DnsName? left, DnsName? right) => !(left == right);

    public static explicit operator DnsName(string name) => Parse(name);

    public static explicit operator string(DnsName name) => name.ToString();
}