
using System.Reflection.Emit;

namespace YABFcompiler.DIL
{
    using System.Diagnostics;

    [DebuggerDisplay("Mul => Offset: {Offset}, Scalar = {Scalar}")]
    class MultiplicationMemoryOp : DILInstruction
    {
        // TODO: I guess this class needs a Constant as well, but I haven't yet emitted a multiplication

        public int Offset { get; set; }
        public int Scalar { get; set; }

        public MultiplicationMemoryOp(int offset, int scalar)
        {
            Offset = offset;
            Scalar = scalar;
        }

        /// <summary>
        /// Given an offset of 2 and scalar of 3, generates:
        /// chArray[index + 2] = (char) (chArray[index + 2] + ((char) (chArray[index] * '\x0003')));
        /// 
        /// If the scalar is 1, no multiplication is done:
        /// chArray[index + 2] = (char) (chArray[index + 2] + chArray[index]);
        /// </summary>
        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);

            if (Offset != 0)
            {
                OpCode instruction;
                int os = Offset;
                if (Offset > 0)
                {
                    instruction = OpCodes.Add;
                }
                else
                {
                    instruction = OpCodes.Sub;
                    os = -os;
                }

                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, os);
                ilg.Emit(instruction);
            }

            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            if (Offset != 0)
            {
                OpCode instruction;
                int os = Offset;
                if (Offset > 0)
                {
                    instruction = OpCodes.Add;
                }
                else
                {
                    instruction = OpCodes.Sub;
                    os = -os;
                }

                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, os);
                ilg.Emit(instruction);
            }

            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelem_U2);
            if (Scalar != 1) // multiply only if the scalar is != 1
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Scalar);
                ilg.Emit(OpCodes.Mul);
                ilg.Emit(OpCodes.Conv_U2);
            }

            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }
    }
}
