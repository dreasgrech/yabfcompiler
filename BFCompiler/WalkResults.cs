using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler
{
    class WalkResults
    {
        public SortedDictionary<uint, int> Domain { get; private set; }
        public uint EndPtrPosition { get; private set; }
        public int TotalInstructionsCovered { get; private set; }

        public WalkResults(SortedDictionary<uint, int> domain, uint endPtrPosition, int totalInstructionsCovered)
        {
            Domain = domain;
            EndPtrPosition = endPtrPosition;
            TotalInstructionsCovered = totalInstructionsCovered;
        }
    }
}
