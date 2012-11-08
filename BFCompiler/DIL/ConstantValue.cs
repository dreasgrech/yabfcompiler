
namespace YABFcompiler.DIL
{
    class ConstantValue
    {
        public int Value { get; private set; }
        public ConstantValue(int constant)
        {
            Value = constant;
        }
    }
}
