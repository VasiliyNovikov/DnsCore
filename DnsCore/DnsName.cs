using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnsCore
{
    public class DnsName : IEquatable<DnsName>
    {
        public IReadOnlyList<DnsLabel> Labels { get; }

        public DnsName(IReadOnlyCollection<DnsLabel> labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            if (labels.Count == 0)
                throw new ArgumentException(Errors.Name_NumberOfLabelsIsZero, nameof(labels));

            Labels = labels.ToList();
        }

        public DnsName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            var labelStrings = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var labels = new DnsLabel[labelStrings.Length];
            for (var i = 0; i < labels.Length; ++i)
                labels[i] = new DnsLabel(labelStrings[i]);
            Labels = labels;
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

        public override bool Equals(object? obj) => obj is DnsName name && this.Equals(name);

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
