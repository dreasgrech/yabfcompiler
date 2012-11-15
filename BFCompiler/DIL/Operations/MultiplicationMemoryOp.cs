
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Diagnostics;
    using System.Reflection.Emit;

    [DebuggerDisplay("Mul => Offset: {Offset}, Scalar = {(char)Scalar}")]
    class MultiplicationMemoryOp : DILInstruction, IOffsettable
    {
        // TODO: I guess this class needs a Constant as well, but I haven't yet emitted a multiplication

        public int Offset { get; set; }
        public int Scalar { get; set; }
        public ConstantValue Constant { get; set; }
        public ConstantValue MultiplicationConstant { get; set; }

        public MultiplicationMemoryOp(int offset, int scalar, ConstantValue constant, ConstantValue multiplicationConstant)
        {
            Offset = offset;
            Scalar = scalar;
            Constant = constant;
            MultiplicationConstant = multiplicationConstant;
        }

        public MultiplicationMemoryOp(int offset, int scalar): this (offset, scalar, null, null)
        {

        }

        /// <summary>
        /// Given an offset of 2 and scalar of 3, generates:
        /// buffer[index + 2] = (byte) (buffer[index + 2] + (buffer[index] * 3));
        /// 
        /// If the scalar is 1, no multiplication is done:
        /// buffer[index + 2] = (byte) (buffer[index + 2] + buffer[index]);
        /// </summary>
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
                if (Offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(Offset));
                    if (Offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldloc, array);
            if (Constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (Offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(Offset));
                    if (Offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldelem_U1);
            ilg.Emit(OpCodes.Ldloc, array);
            if (MultiplicationConstant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, MultiplicationConstant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }

            ilg.Emit(OpCodes.Ldelem_U1);
            if (Scalar != 1) // multiply only if the scalar is != 1
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Scalar);
                ilg.Emit(OpCodes.Mul);
            }

            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U1); // Cast the whole expression to byte
            ilg.Emit(OpCodes.Stelem_I1);
        }
    }
}
