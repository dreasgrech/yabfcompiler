
namespace YABFcompiler.DIL
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Read => Offset: {Offset}, Count: {Repeated}")]
    internal class ReadOp : DILInstruction, IOffsettable
    {
        public int Offset { get; set; }
        public int Repeated { get; private set; }
        public ConstantValue Constant { get; private set; }

        private readonly MethodInfo consoleReadMethodInfo = typeof(Console).GetMethod("Read");

        public ReadOp(int offset, int repeated):this(offset, repeated, null)
        {

        }

        public ReadOp(int offset, int repeated, ConstantValue constant)
        {
            Offset = offset;
            Constant = constant;
            Repeated = repeated;
        }

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            for (int i = 0; i < Repeated; i++)
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

                ilg.EmitCall(OpCodes.Call, consoleReadMethodInfo, null);
                ilg.Emit(OpCodes.Conv_U2);
                ilg.Emit(OpCodes.Stelem_I2);
            }
        }
    }
}
