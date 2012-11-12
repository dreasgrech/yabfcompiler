
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

        /// <summary>
        /// The state of the domain after the string walk
        /// </summary>
        public Dictionary<int, char> Domain { get; private set; }

        public StringWalkResults(List<string> strings, Dictionary<int, char> domain)
        {
            Strings = strings;
            Domain = domain;
        }
    }
}
