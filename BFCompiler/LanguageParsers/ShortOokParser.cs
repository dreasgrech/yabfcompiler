
namespace YABFcompiler.LanguageParsers
{
    using System.Collections.Generic;

    internal class ShortOokParser : Parser
    {
        public ShortOokParser()
            : base(
            new Dictionary<string, DILInstruction> { { ".?", DILInstruction.IncPtr }, { "?.", DILInstruction.DecPtr }, { "..", DILInstruction.Inc }, { "!!", DILInstruction.Dec }, { "!.", DILInstruction.Output }, { ".!", DILInstruction.Input }, { "!?", DILInstruction.StartLoop }, { "?!", DILInstruction.EndLoop } })
        {
;
        }
    }
}
