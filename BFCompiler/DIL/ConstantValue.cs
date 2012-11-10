
using System.Diagnostics;

namespace YABFcompiler.DIL
{
    /// <summary>
    /// Used during Constant Substitution 
    /// </summary>
    [DebuggerDisplay("Constant => Value: {Value}")]
    class ConstantValue
    {
        public int Value { get; private set; }
        public ConstantValue(int constant)
        {
            Value = constant;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
