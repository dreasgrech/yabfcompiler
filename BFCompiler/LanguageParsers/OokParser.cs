
namespace YABFcompiler.LanguageParsers
{
    using System.Collections.Generic;

    internal class OokParser : Parser
    {
        public OokParser() : base(
            new Dictionary<string, LanguageInstruction> { { "Ook.Ook?", LanguageInstruction.IncPtr }, { "Ook?Ook.", LanguageInstruction.DecPtr }, { "Ook.Ook.", LanguageInstruction.Inc }, { "Ook!Ook!", LanguageInstruction.Dec }, { "Ook!Ook.", LanguageInstruction.Output }, { "Ook.Ook!", LanguageInstruction.Input }, { "Ook!Ook?", LanguageInstruction.StartLoop }, { "Ook?Ook!", LanguageInstruction.EndLoop } })
        {
;
        }
    }
}
