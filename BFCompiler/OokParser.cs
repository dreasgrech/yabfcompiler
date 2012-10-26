using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    class OokParser : Parser
    {
        public OokParser() : base(
            new Dictionary<string, DILInstruction> { { "Ook. Ook?", DILInstruction.IncPtr }, { "Ook? Ook.", DILInstruction.DecPtr }, { "Ook. Ook.", DILInstruction.Inc }, { "Ook! Ook!", DILInstruction.Dec }, { "Ook! Ook.", DILInstruction.Output }, { "Ook. Ook!", DILInstruction.Input }, { "Ook! Ook?", DILInstruction.StartLoop }, { "Ook? Ook!", DILInstruction.EndLoop } },
            9)
        {
;
        }
    }
}
