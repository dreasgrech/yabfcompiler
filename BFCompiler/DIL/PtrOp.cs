
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
    }
}
