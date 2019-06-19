using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnsCore
{
    public class Name
    {
        public IReadOnlyList<Label> Labels { get; }

        public Name(IReadOnlyCollection<Label> labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            if (labels.Count == 0)
                throw new ArgumentException("Number of labels must be greater than 0", nameof(labels));

            Labels = labels.ToList();
        }

        public Name(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            var labelStrings = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var labels = new Label[labelStrings.Length];
            for (var i = 0; i < labels.Length; ++i)
                labels[i] = labelStrings[i];
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

        public static implicit operator Name(string name) => new Name(name);
        public static implicit operator string(Name name) => name.ToString();
    }
}
