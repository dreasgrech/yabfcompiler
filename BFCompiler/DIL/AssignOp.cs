
namespace YABFcompiler.DIL
{
    using System.Diagnostics;
    using System.Reflection.Emit;

    /// <summary>
    /// Assign an integer constant
    /// </summary>
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

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (Constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }

            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Value);
            ilg.Emit(OpCodes.Stelem_I2);
        }
    }
}
