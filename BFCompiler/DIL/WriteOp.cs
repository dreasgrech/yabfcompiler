
namespace YABFcompiler.DIL
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Output")]
    class WriteOp : DILInstruction
    {
        public ConstantValue Constant { get; private set; }

        private static readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(char) });

        public WriteOp()
        {
            
        }

        public WriteOp(ConstantValue constant)
        {
            Constant = constant;
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

            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
        }
    }
}
