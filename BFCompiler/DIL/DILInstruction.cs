
namespace YABFcompiler.DIL
{
    using System.Reflection.Emit;

    interface DILInstruction
    {
        void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr);
    }
}
