
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class LoopOp:DILInstruction
    {
        public Loop Loop { get; private set; }

        public LoopOp(Loop loop)
        {
            Loop = loop;
        }

        public List<DILInstruction> Unroll()
        {
            var unrolled = new List<DILInstruction>();
            foreach (var cell in Loop.WalkResults.Domain)
            {
                if (cell.Key == 0)
                {
                    continue;
                }

                unrolled.Add(new MultiplicationMemoryOp(cell.Key, cell.Value));
            }

            if(Loop.WalkResults.Domain.ContainsKey(0))
            {
                unrolled.Add(new AssignOp(0, 0));
            }

            return unrolled;
        } 

    }
}
