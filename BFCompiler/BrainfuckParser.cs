using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    class BrainfuckParser : Parser
    {
        public BrainfuckParser() : base(
            new Dictionary<string, DILInstruction> { { ">", DILInstruction.IncPtr }, { "<", DILInstruction.DecPtr }, { "+", DILInstruction.Inc }, { "-", DILInstruction.Dec }, { ".", DILInstruction.Output }, { ",", DILInstruction.Input }, { "[", DILInstruction.StartLoop }, { "]", DILInstruction.EndLoop } } ,
            1)
        {
        }
    }
}