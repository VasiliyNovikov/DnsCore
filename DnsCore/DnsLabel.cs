using System;
using System.Globalization;

namespace DnsCore
{
    public class DnsLabel : IEquatable<DnsLabel>
    {
        private const int MaxLength = 63;

        private readonly string _label;

        public int Length => _label.Length;

        public DnsLabel(string label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            if (label.Length < 1)
                throw new ArgumentException(Errors.Label_LengthShouldBeAtLeastOne, nameof(label));
            if (label.Length > MaxLength)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Errors.Label_LengthShouldNotBeMoreThanMaxFormat, MaxLength), nameof(label));

            static bool IsLetter(char c) => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
            static bool IsLetterOrDigit(char c) => IsLetter(c) || c >= '0' && c <= '9';
            static bool IsLetterOrDigitOrHyphen(char c) => IsLetterOrDigit(c) || c == '-';

            if (!IsLetter(label[0]))
                throw new ArgumentException(Errors.Label_ShouldStartWithLetter, nameof(label));

            if (label.Length > 1)
            {
                for (var i = 1; i < label.Length - 1; ++i)
                    if (!IsLetterOrDigitOrHyphen(label[i]))
                        throw new ArgumentException(Errors.Label_CanContainLettersOrDigitsOrHyphen, nameof(label));

                if (!IsLetterOrDigit(label[label.Length - 1]))
                    throw new ArgumentException(Errors.Label_ShouldEndWithLetterOrDigit, nameof(label));
            }

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