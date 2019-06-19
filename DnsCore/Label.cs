using System;

namespace DnsCore
{
    public class Label
    {
        private const int MaxLength = 63;

        private readonly string _label;

        public Label(string label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            if (label.Length > MaxLength)
                throw new ArgumentException($"Label should have length less then or equal to {MaxLength}", nameof(label));

            _label = label ?? String.Empty;
        }

        public override string ToString() => _label;

        public static implicit operator Label(string label) => new Label(label);
        public static implicit operator string(Label label) => label._label;
    }
}
