
namespace YABFcompiler.DIL
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Input")]
    internal class ReadOp : DILInstruction
    {
        private readonly MethodInfo consoleReadMethodInfo = typeof(Console).GetMethod("Read");

        public ConstantValue Constant { get; private set; }

        public ReadOp()
        {

        }

        public ReadOp(ConstantValue constant)
        {
            Constant = constant;
        }

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, ConstantValue constant = null)
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

            ilg.EmitCall(OpCodes.Call, consoleReadMethodInfo, null);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }
    }
}
