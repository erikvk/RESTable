using System.Collections.Generic;

namespace RESTar.Deflection.IL
{
    internal sealed class MatchContext
    {
        internal Instruction instruction;
        private readonly Dictionary<object, object> data = new Dictionary<object, object>();
        internal bool success;

        internal bool IsMatch
        {
            get => success;
            set => success = true;
        }

        internal MatchContext(Instruction instruction) => Reset(instruction);
        internal bool TryGetData(object key, out object value) => data.TryGetValue(key, out value);
        internal void AddData(object key, object value) => data.Add(key, value);

        internal void Reset(Instruction instruction)
        {
            this.instruction = instruction;
            success = true;
        }

        internal void Advance() => instruction = instruction.Next;
    }
}