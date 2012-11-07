
using System;

namespace YABFcompiler
{
    using System.Collections.Generic;

    class WalkResults
    {
        /// <summary>
        /// Starting from a clean slate of 0, this is the state of domain after the walk
        /// 
        /// The key contains the relative cell number (so 0 indicates the changes done on the cell where the walk started, etc...)
        /// The value contains the changed (incremented or decremented) value of the cell
        /// </summary>
        public SortedDictionary<int, int> Domain { get; private set; }

        /// <summary>
        /// The relative position of the pointer after the walk
        /// </summary>
        public int EndPtrPosition { get; private set; }

        /// <summary>
        /// The total number of operations covered during this walk
        /// </summary>
        public int TotalInstructionsCovered { get; private set; }


        public WalkResults(SortedDictionary<int, int> domain, int endPtrPosition, int totalInstructionsCovered)
        {
            Domain = domain;
            EndPtrPosition = endPtrPosition;
            TotalInstructionsCovered = totalInstructionsCovered;
        }

        public void IterateDomain(Action<int,int> callback)
        {
            foreach (var cellAndValue in Domain)
            {
                if (cellAndValue.Key != 0)
                {
                    callback(cellAndValue.Key, cellAndValue.Value);
                }
            }

            // If there is no change on 0, then it's an infinite loop
            if (Domain.ContainsKey(0))
            {
                callback(0, Domain[0]);
            }
        }
    }
}
