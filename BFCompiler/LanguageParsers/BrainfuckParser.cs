
namespace YABFcompiler.LanguageParsers
{
    using System.Collections.Generic;

    internal class BrainfuckParser : Parser
    {
        public BrainfuckParser() : base(
            new Dictionary<string, LanguageInstruction> { { ">", LanguageInstruction.IncPtr }, { "<", LanguageInstruction.DecPtr }, { "+", LanguageInstruction.Inc }, { "-", LanguageInstruction.Dec }, { ".", LanguageInstruction.Output }, { ",", LanguageInstruction.Input }, { "[", LanguageInstruction.StartLoop }, { "]", LanguageInstruction.EndLoop } })
        {
        }
    }
}