using System;
using System.Globalization;

namespace DnsCore
{
    public class Label : IEquatable<Label>
    {
        private const int MaxLength = 63;

        private readonly string _label;

        public Label(string label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            if (label.Length > MaxLength)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Errors.Label_LengthShouldNotBeMoreThanMaxFormat, MaxLength), nameof(label));

            _label = label;
        }

        public override string ToString() => _label;

        public override bool Equals(object? obj) => obj is Label label && Equals(label);

        public bool Equals(Label? other) => other is object && _label.Equals(other._label, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(_label);

        public static bool operator ==(Label left, Label right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Label left, Label right) => !(left == right);
    }
}
