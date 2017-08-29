using System;
#pragma warning disable 1591

namespace RESTar
{
    /// <summary>
    /// Encodes an operator, used in conditions
    /// </summary>
    public struct Operator
    {
        /// <summary>
        /// The code for this operator
        /// </summary>
        public readonly Operators OpCode;

        internal string Common => GetString(OpCode);
        internal bool Equality => OpCode == Operators.EQUALS || OpCode == Operators.NOT_EQUALS;
        internal bool Compare => !Equality;
        internal string SQL => OpCode == Operators.NOT_EQUALS ? "<>" : Common;
        public override bool Equals(object obj) => obj is Operator && (Operator) obj == OpCode;
        public bool Equals(Operator other) => OpCode == other.OpCode;
        public override int GetHashCode() => (int) OpCode;
        public override string ToString() => Common;
        internal static readonly string[] AvailableOperators = {"=", "!=", "<", ">", "<=", ">="};
        public static bool operator ==(Operator o1, Operator o2) => o1.OpCode == o2.OpCode;
        public static bool operator !=(Operator o1, Operator o2) => !(o1 == o2);

        public static Operator EQUALS => Operators.EQUALS;
        public static Operator NOT_EQUALS => Operators.NOT_EQUALS;
        public static Operator LESS_THAN => Operators.LESS_THAN;
        public static Operator GREATER_THAN => Operators.GREATER_THAN;
        public static Operator LESS_THAN_OR_EQUALS => Operators.LESS_THAN_OR_EQUALS;
        public static Operator GREATER_THAN_OR_EQUALS => Operators.GREATER_THAN_OR_EQUALS;

        internal Operator(Operators op) => OpCode = op;

        internal static string GetString(Operators op)
        {
            switch (op)
            {
                case Operators.EQUALS: return "=";
                case Operators.NOT_EQUALS: return "!=";
                case Operators.LESS_THAN: return "<";
                case Operators.GREATER_THAN: return ">";
                case Operators.LESS_THAN_OR_EQUALS: return "<=";
                case Operators.GREATER_THAN_OR_EQUALS: return ">=";
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal static Operator Parse(string common)
        {
            switch (common)
            {
                case "=": return new Operator(Operators.EQUALS);
                case "!=": return new Operator(Operators.NOT_EQUALS);
                case "<": return new Operator(Operators.LESS_THAN);
                case ">": return new Operator(Operators.GREATER_THAN);
                case "<=": return new Operator(Operators.LESS_THAN_OR_EQUALS);
                case ">=": return new Operator(Operators.GREATER_THAN_OR_EQUALS);
                default: throw new ArgumentException(nameof(common));
            }
        }

        internal static bool TryParse(string common, out Operator op)
        {
            try
            {
                op = Parse(common);
                return true;
            }
            catch
            {
                op = default(Operator);
                return false;
            }
        }

        public static implicit operator Operator(string op) => TryParse(op, out var o) ? o : Operators.nil;
        public static implicit operator Operator(Operators op) => new Operator(op);
    }
}