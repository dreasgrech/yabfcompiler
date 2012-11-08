
namespace YABFcompiler.DIL
{
    /// <summary>
    /// Used during Constant Substitution 
    /// </summary>
    class ConstantValue
    {
        public int Value { get; private set; }
        public ConstantValue(int constant)
        {
            Value = constant;
        }
    }
}
