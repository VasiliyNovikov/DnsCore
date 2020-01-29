using System;
using System.Globalization;

namespace DnsCore
{
    public class DnsLabel : IEquatable<DnsLabel>
    {
        private const int MaxLength = 63;

        private readonly string _label;

        public DnsLabel(string label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            if (label.Length > MaxLength)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Errors.Label_LengthShouldNotBeMoreThanMaxFormat, MaxLength), nameof(label));

            _label = label;
        }

        public override string ToString() => _label;

        public override bool Equals(object? obj) => obj is DnsLabel label && Equals(label);

        public bool Equals(DnsLabel? other) => other is object && _label.Equals(other._label, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(_label);

        public static bool operator ==(DnsLabel left, DnsLabel right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(DnsLabel left, DnsLabel right) => !(left == right);
    }
}
