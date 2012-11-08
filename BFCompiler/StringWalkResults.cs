using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler
{
    class StringWalkResults
    {
        /// <summary>
        /// The total number of operations covered during this walk
        /// </summary>
        public int TotalInstructionsCovered { get; private set; }

        public Dictionary<int, string> Strings { get; private set; }

        public StringWalkResults(Dictionary<int,string> strings)
        {
            Strings = strings;
        }
    }
}
