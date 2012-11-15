
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

    [DebuggerDisplay("Read => Offset: {Offset}, Count: {Repeated}")]
    internal class ReadOp : DILInstruction, IOffsettable, IRepeatable
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

        /// <summary>
        /// Given an offset of 2, generates:
        /// buffer[index + 2] = (byte) Console.Read();
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

                ilg.EmitCall(OpCodes.Call, consoleReadMethodInfo, null);
                ilg.Emit(OpCodes.Conv_U1);
                ilg.Emit(OpCodes.Stelem_I1);
            }
        }

        /// <summary>
        /// I think I've never tested this method before
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public bool Repeat(DILOperationSet operations, int offset)
        {
            int repeated = Repeated, totalOperationsCovered = 1;
            for (int i = offset + 1; i < operations.Count; i++)
            {
                var instruction = operations[i] as ReadOp;
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
                operations.Insert(offset, new ReadOp(Offset, repeated));
                return true;
            }

            return false;
        }
    }
}
