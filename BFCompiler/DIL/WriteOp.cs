
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

        private readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(char) });

        public WriteOp()
        {
            
        }

        public WriteOp(ConstantValue constant)
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

            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
        }
    }
}
