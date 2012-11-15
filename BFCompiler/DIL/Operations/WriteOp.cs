
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Write => Offset: {Offset}, Count: {Repeated}, Constant: {Constant}")]
    class WriteOp : IRepeatable, DILInstruction, IOffsettable
    {
        public int Offset { get; set; }
        public int Repeated { get; private set; }
        public ConstantValue Constant { get; private set; }

        private static readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(char) });

        public WriteOp(int offset, int repeated): this(offset, repeated, null)
        {
            
        }

        public WriteOp(int offset, int repeated, ConstantValue constant)
        {
            Offset = offset;
            Constant = constant;
            Repeated = repeated;
        }

        /// <summary>
        /// Given an offset of 2, generates:
        /// Console.Write((char) buffer[index + 2]);
        /// 
        /// TODO: This method is missing the Offset usage
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="array"></param>
        /// <param name="ptr"></param>
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

                ilg.Emit(OpCodes.Ldelem_U1);
                ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
            }
        }

        public bool Repeat(DILOperationSet operations, int offset)
        {
            int repeated = Repeated, totalOperationsCovered = 1;
            for (int i = offset + 1; i < operations.Count; i++)
            {
                var instruction = operations[i] as WriteOp;
                if (instruction == null)
                {
                    break;
                }

                if (instruction.Offset != Offset)
                {
                    break;
                }

                repeated += instruction.Repeated;
                totalOperationsCovered++;
            }

            if (totalOperationsCovered > 1)
            {
                operations.RemoveRange(offset, totalOperationsCovered);
                operations.Insert(offset, new WriteOp(Offset, repeated));
                return true;
            }

            return false;
        }
    }
}
