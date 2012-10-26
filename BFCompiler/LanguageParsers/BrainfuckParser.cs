
namespace YABFcompiler.LanguageParsers
{
    using System.Collections.Generic;

    class BrainfuckParser : Parser
    {
        public BrainfuckParser() : base(
            new Dictionary<string, DILInstruction> { { ">", DILInstruction.IncPtr }, { "<", DILInstruction.DecPtr }, { "+", DILInstruction.Inc }, { "-", DILInstruction.Dec }, { ".", DILInstruction.Output }, { ",", DILInstruction.Input }, { "[", DILInstruction.StartLoop }, { "]", DILInstruction.EndLoop } })
        {
        }
    }
}