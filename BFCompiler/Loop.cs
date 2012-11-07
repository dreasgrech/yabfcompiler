
namespace YABFcompiler
{
    using System.Collections.Generic;
    using System.Linq;

    class Loop
    {
        public int Index { get; set; }
        public DILInstruction[] Instructions { get; set; }
        public List<Loop> NestedLoops { get; set; }

        public Loop(int index, IEnumerable<DILInstruction> instructions, List<Loop> nestedLoops)
        {
            Index = index;
            Instructions = instructions.ToArray();
            NestedLoops = nestedLoops;
        }

        /// <summary>
        /// Constructs a Loop given a set of instructions and 
        /// an offset of the position of where the loop starts
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="offset">The index of the StartLoop instruction</param>
        /// <returns></returns>
        public static Loop Construct(DILInstruction[] instructions, int offset)
        {
            var loopInstructions = GetLoopInstructions(instructions, offset);
            var nestedLoops = GetNestedLoops(loopInstructions);

            return new Loop(offset, loopInstructions, nestedLoops);
        }

        /// <summary>
        /// If this loop is [++>[--]>-], then this method will return ++> >-
        /// i.e. the 5 operations that are not within nested loops
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DILInstruction> GetInstructionsSkippingNestedLoops()
        {
            for (int i = 0; i < Instructions.Length; i++)
            {
                var instruction = Instructions[i];
                if (!(instruction == DILInstruction.StartLoop || instruction == DILInstruction.EndLoop))
                {
                    yield return instruction;
                }
                else
                {
                    i = GetNextClosingLoopIndex(i).Value;
                }
            }
        }

        /// <summary>
        /// Returns true if a clearance pattern is detected with this loop
        /// 
        /// The following patterns are currently detected:
        ///     [-], [+]
        /// </summary>
        /// <returns></returns>
        public bool IsClearanceLoop()
        {
            if (Instructions.Length == 1) // [-] or [+]
            {
                if (Instructions[0] == DILInstruction.Dec || Instructions[0] == DILInstruction.Inc)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if an infinite loop pattern is detected with this loop
        /// </summary>
        /// <returns></returns>
        public bool IsInfiniteLoopPattern()
        {
            if (Instructions.Length == 0) // [] can be an infinite loop if it starts on a cell which is not 0, otherwise it's skipped
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Given a set of instructions and an offset of where the loop starts,
        /// this method returns the operations that the loop contains
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="offset">The index of the StartLoop instruction</param>
        /// <returns></returns>
        private static DILInstruction[] GetLoopInstructions(DILInstruction[] instructions, int offset)
        {
            var closingEndLoopIndex = GetNextClosingLoopIndex(instructions, offset).Value;
            return instructions.Skip(offset + 1).Take(closingEndLoopIndex - offset - 1).ToArray();
        }

        /// <summary>
        /// Given a set of instructions, this method returns a collection of 
        /// nested loops contained in the set.
        /// </summary>
        /// <returns></returns>
        private static List<Loop> GetNestedLoops(DILInstruction[] instructions)
        {
            var nestedLoops = new List<Loop>();
            var loopInstructions = instructions;
            var containsNestedLoops = loopInstructions.Any(li => li == DILInstruction.StartLoop);
            if (!containsNestedLoops)
            {
                return nestedLoops;
            }

            for (int i = 0; i < instructions.Length; i++)
            {
                var ins = instructions[i];
                if (!(ins == DILInstruction.StartLoop || ins == DILInstruction.EndLoop))
                {
                    continue;
                }

                if (ins == DILInstruction.StartLoop)
                {
                    var lInstructions = GetLoopInstructions(instructions, i);
                    var loop = new Loop(i, lInstructions, GetNestedLoops(lInstructions));
                    nestedLoops.Add(loop);

                    i = GetNextClosingLoopIndex(instructions, i).Value;
                }
            }

            return nestedLoops;
        }

        private static int? GetNextClosingLoopIndex(DILInstruction[] instructions, int index)
        {
            int stack = 0;

            for (int i = index + 1; i < instructions.Length; i++)
            {
                if (instructions[i] == DILInstruction.StartLoop)
                {
                    stack += 1;
                }

                if (instructions[i] == DILInstruction.EndLoop)
                {
                    if (stack > 0)
                    {
                        stack--;
                        continue;
                    }

                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if this loop and all it's nested loops are simple loops
        /// </summary>
        /// <returns></returns>
        public bool IsSimple()
        {
            var areNestedLoopsSimple = IsSimpleLoopExcludingNestedLoops();
            foreach (var nestedLoop in NestedLoops)
            {
                areNestedLoopsSimple &= nestedLoop.IsSimpleLoopExcludingNestedLoops();
            }

            return areNestedLoopsSimple;
        }

        /// <summary>
        /// Returns true if the instructions of this loop (excluding the nested loops):
        ///     a) do not contain any IO
        ///     b) and the pointer returns to the startion point after execution. 
        ///        Meaning that the position of StartLoop is equal to the position of EndLoop.
        /// </summary>
        /// <returns></returns>
        private bool IsSimpleLoopExcludingNestedLoops()
        {
            var instructionsWithoutNestedLoops = GetInstructionsSkippingNestedLoops().ToArray();

            bool containsIO = instructionsWithoutNestedLoops.Any(i => i == DILInstruction.Input || i == DILInstruction.Output);

            if (containsIO)
            {
                return false;
            }


            int totalIncPtrs = instructionsWithoutNestedLoops.Count(i => i == DILInstruction.IncPtr),
                totalDecPtrs = instructionsWithoutNestedLoops.Count(i => i == DILInstruction.DecPtr);

            var returnsToStartLoopPosition = totalDecPtrs == totalIncPtrs;

            if (!returnsToStartLoopPosition)
            {
                return false;
            }

            return true;
        }



        /// <summary>
        /// Returns the index of the EndLoop for the given StartLoop
        /// 
        /// Returns null if a matching EndLoop is not found
        /// </summary>
        /// <param name="index">The index of the StartLoop instruction</param>
        /// <returns></returns>
        private int? GetNextClosingLoopIndex(int index)
        {
            return GetNextClosingLoopIndex(Instructions, index);
        }
    }
}
