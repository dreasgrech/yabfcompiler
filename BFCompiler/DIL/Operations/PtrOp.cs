
namespace YABFcompiler.DIL.Operations
{
    using System.Diagnostics;
    using System.Reflection.Emit;

    [DebuggerDisplay("Ptr => Delta = {Delta}")]
    class PtrOp : DILInstruction, IRepeatable
    {
        public int Delta { get; set; }

        public PtrOp(int delta)
        {
            Delta = delta;
        }

        /// <summary>
        /// Given a delta of 3, generates:
        /// ptr += 3;
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="array"></param>
        /// <param name="ptr"></param>
        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            if (Delta > 0)
            {
                IncrementPtr(ilg, ptr);
            }
            else
            {
                DecrementPtr(ilg, ptr);
            }
        }

        /// <summary>
        /// Emit instructions to increment the pointer position by an integer constant
        /// </summary>
        private void IncrementPtr(ILGenerator ilg, LocalBuilder ptr)
        {
            ilg.Increment(ptr, Delta);
        }

        /// <summary>
        /// Emit instructions to decrement the pointer position by an integer constant
        /// </summary>
        private void DecrementPtr(ILGenerator ilg, LocalBuilder ptr)
        {
            ilg.Decrement(ptr, -Delta);
        }

        public bool Repeat(DILOperationSet operations, int offset)
        {
            int ptrDelta = Delta, totalPtrsCovered = 1;

            for (int j = offset + 1; j < operations.Count; j++)
            {
                var instruction = operations[j] as PtrOp;
                if (instruction == null)
                {
                    break;
                }

                ptrDelta += instruction.Delta;
                totalPtrsCovered++;
            }

            if (totalPtrsCovered > 1)
            {
                operations.RemoveRange(offset, totalPtrsCovered);
                operations.Insert(offset, new PtrOp(ptrDelta));
                return true;
            }

            return false;
        }
    }
}
