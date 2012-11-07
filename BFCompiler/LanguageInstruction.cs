
namespace YABFcompiler
{
    using System;

    /// <summary>
    /// A representation for the language instructions
    /// 
    /// I'm using the bitwise complement operator to denote opposites
    /// </summary>
    [Flags]
    public enum LanguageInstruction
    {
        IncPtr = 0x1,
        DecPtr = ~IncPtr,
        Inc = 0x2,
        Dec = ~Inc,
        Output = 0x4,
        Input = ~Output,
        StartLoop = 0x8,
        EndLoop = ~StartLoop
    }
}
