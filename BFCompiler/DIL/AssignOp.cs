
namespace YABFcompiler.DIL
{
    using System.Diagnostics;
    using System.Reflection.Emit;

    [DebuggerDisplay("Ass => Offset: {Offset}, Value = {Value}")]
    class AssignOp : DILInstruction
    {
        public int Offset { get; set; }
        public int Value { get; set; }
        public ConstantValue Constant { get; set; }

        public AssignOp(int offset, int value, ConstantValue constant)
        {
            Offset = offset;
            Value = value;
            Constant = constant;
        }

        public AssignOp(int offset, int value) : this(offset, value, null)
        {
            
        }

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, ConstantValue constant = null, int value = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }

            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, value);
            ilg.Emit(OpCodes.Stelem_I2);
        }
    }
}
