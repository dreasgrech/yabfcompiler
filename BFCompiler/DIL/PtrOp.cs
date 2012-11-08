
namespace YABFcompiler.DIL
{
    using System.Diagnostics;
    using System.Reflection.Emit;

    [DebuggerDisplay("Ptr => Delta = {Delta}")]
    class PtrOp : DILInstruction
    {
        public int Delta { get; set; }

        public PtrOp(int delta)
        {
            Delta = delta;
        }

        public void Emit(ILGenerator ilg, LocalBuilder ptr)
        {
            if (Delta > 0)
            {
                IncrementPtr(ilg, ptr, Delta);
            }
            else
            {
                DecrementPtr(ilg, ptr, -Delta);
            }
        }

        /// <summary>
        /// Emit instructions to increment the pointer position by an integer constant
        /// </summary>
        private void IncrementPtr(ILGenerator ilg, LocalBuilder ptr, int step = 1)
        {
            ilg.Increment(ptr, step);
        }

        /// <summary>
        /// Emit instructions to decrement the pointer position by an integer constant
        /// </summary>
        private void DecrementPtr(ILGenerator ilg, LocalBuilder ptr, int step = 1)
        {
            ilg.Decrement(ptr, step);
        }
    }
}
