
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Diagnostics;
    using System.Reflection.Emit;

    /// <summary>
    /// Assign an integer constant
    /// </summary>
    [DebuggerDisplay("Ass => Offset: {Offset}, Value = {(char)Value}, Constant: {Constant}")]
    class AssignOp : DILInstruction, IOffsettable, IRepeatable, IInterpretable
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

        /// <summary>
        /// Given an offset of 3 and a value of 2, generates:
        /// buffer[index + 3] = 2;
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="array"></param>
        /// <param name="ptr"></param>
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

            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Value);
            ilg.Emit(OpCodes.Stelem_I1);
        }

        public bool Repeat(DILOperationSet operations, int offset)
        {
            var totalOperationsCovered = 1;
            for (int j = offset + 1; j < operations.Count; j++)
            {
                var instruction = operations[j] as AssignOp;
                if (instruction == null)
                {
                    break;
                }

                if (instruction.Offset != Offset)
                {
                    break;
                }

                if (instruction.Value != Value)
                {
                    break;
                }

                totalOperationsCovered++;
            }

            if (totalOperationsCovered > 1)
            {
                operations.RemoveRange(offset, totalOperationsCovered);
                operations.Insert(offset, new AssignOp(Offset, Value));

                return true;

            }

            return false;
        }

        public void Interpret(byte[] domain, ref int ptr)
        {
            domain[ptr + Offset] = (byte)Value;
        }
    }
}
