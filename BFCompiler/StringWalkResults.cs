
namespace YABFcompiler
{
    using System.Collections.Generic;

    class StringWalkResults
    {
        /// <summary>
        /// The total number of operations covered during this walk
        /// </summary>
        public int TotalInstructionsCovered { get; private set; }

        public List<string> Strings { get; private set; }

        public StringWalkResults(List<string> strings)
        {
            Strings = strings;
        }
    }
}
