using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DnsCore
{
    public class DnsName : IEquatable<DnsName>
    {
        private const int MaxLength = 255;

        public IReadOnlyList<DnsLabel> Labels { get; }

        public int Length { get; }

        private DnsName(DnsLabel[]? labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            
            var length = 0;
            foreach (var label in labels)
                length += label.Length;

            if (length > MaxLength)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Errors.Name_LengthShouldNotBeMoreThanMaxFormat, MaxLength), nameof(labels));

            Labels = labels;
            Length = length;
        }

        public DnsName(IReadOnlyCollection<DnsLabel> labels)
            : this(labels?.ToArray())
        {
        }

        public DnsName(string name)
            : this(Parse(name))
        {
        }

        private static DnsLabel[] Parse(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var labelStrings = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var labels = new DnsLabel[labelStrings.Length];
            for (var i = 0; i < labels.Length; ++i)
                labels[i] = new DnsLabel(labelStrings[i]);
            return labels;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var label in Labels)
            {
                builder.Append(label);
                builder.Append(".");
            }
            return builder.ToString();
        }

        public bool Equals(DnsName? other)
        {
            if (other is null)
                return false;

            if (Labels.Count != other.Labels.Count)
                return false;

            for (var i = 0; i < Labels.Count; ++i)
                if (Labels[i] != other.Labels[i])
                    return false;

            return true;
        }

        public override bool Equals(object? obj) => obj is DnsName name && Equals(name);

        public override int GetHashCode()
        {
            var result = 0;
            foreach(var label in Labels)
                result = HashCode.Combine(result, label);
            return result;
        }

        public static bool operator ==(DnsName left, DnsName right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(DnsName left, DnsName right) => !(left == right);
    }
}