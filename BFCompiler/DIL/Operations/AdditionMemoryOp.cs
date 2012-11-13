
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Diagnostics;
    using System.Reflection.Emit;

    [DebuggerDisplay("Add => Offset: {Offset}, Scalar = {(char)Scalar}, Constant: {Constant}")]
    class AdditionMemoryOp : DILInstruction, IRepeatable, IOffsettable
    {
        public int Offset { get; set; }
        public int Scalar { get; set; }
        public ConstantValue Constant { get; set; }

        public AdditionMemoryOp(int offset, int scalar, ConstantValue constant)
        {
            Offset = offset;
            Scalar = scalar;
            Constant = constant;
        }

        public AdditionMemoryOp(int offset, int scalar):this(offset, scalar, null)
        {
            
        }

        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            if (Scalar > 0)
            {
                Increment(ilg, array, ptr, Constant, Offset, Scalar);
            }
            else
            {
                Decrement(ilg, array, ptr, Constant, Offset, -Scalar);
            }
        }

        private void Decrement(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, ConstantValue constantValue, int offset, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constantValue != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constantValue.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
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
            if (constantValue != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constantValue.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldelem_U2);
            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Sub);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        private void Increment(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, ConstantValue constant, int offset, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
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
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldelem_U2);
            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        public bool Repeat(DILOperationSet operations, int offset)
        {
            int delta = Scalar, totalOperationsCovered = 1;

            for (int j = offset + 1; j < operations.Count; j++) // - i ?
            {
                var instruction = operations[j] as AdditionMemoryOp;
                if (instruction == null)
                {
                    break;
                }

                if (instruction.Offset != Offset)
                {
                    break;
                }

                totalOperationsCovered++;
                delta += instruction.Scalar;
            }

            if (totalOperationsCovered > 1)
            {
                operations.RemoveRange(offset, totalOperationsCovered);
                operations.Insert(offset, new AdditionMemoryOp(Offset, delta));

                return true;

            }

            return false;
        }
    }
}
