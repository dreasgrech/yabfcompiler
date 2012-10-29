
namespace YABFcompiler.LanguageParsers
{
    using System.Collections.Generic;

    internal class OokParser : Parser
    {
        public OokParser() : base(
            new Dictionary<string, DILInstruction> { { "Ook.Ook?", DILInstruction.IncPtr }, { "Ook?Ook.", DILInstruction.DecPtr }, { "Ook.Ook.", DILInstruction.Inc }, { "Ook!Ook!", DILInstruction.Dec }, { "Ook!Ook.", DILInstruction.Output }, { "Ook.Ook!", DILInstruction.Input }, { "Ook!Ook?", DILInstruction.StartLoop }, { "Ook?Ook!", DILInstruction.EndLoop } })
        {
;
        }
    }
}
