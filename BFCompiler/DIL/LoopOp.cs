
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class LoopOp:DILInstruction
    {
        public IEnumerable<DILInstruction> Instructions { get; private set; }
 
        public LoopOp(IEnumerable<DILInstruction> instructions)
        {
            Instructions = instructions;
        }
    }
}
