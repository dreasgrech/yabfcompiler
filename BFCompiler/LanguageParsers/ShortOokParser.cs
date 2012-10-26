using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.LanguageParsers
{
    class ShortOokParser:Parser
    {
        public ShortOokParser()
            : base(
            new Dictionary<string, DILInstruction> { { ".?", DILInstruction.IncPtr }, { "?.", DILInstruction.DecPtr }, { "..", DILInstruction.Inc }, { "!!", DILInstruction.Dec }, { "!.", DILInstruction.Output }, { ".!", DILInstruction.Input }, { "!?", DILInstruction.StartLoop }, { "?!", DILInstruction.EndLoop } })
        {
;
        }
    }
}
