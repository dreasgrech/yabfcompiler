
using System.Reflection.Emit;

namespace YABFcompiler.DIL
{
    interface DILInstruction
    {
        void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr);
    }
}
