
namespace YABFcompiler
{
    using System.Collections.Generic;
    using System.Linq;

    class Loop
    {
        public int Index { get; set; }
        public LanguageInstruction[] Instructions { get; set; }
        public List<Loop> NestedLoops { get; set; }

        /// <summary>
        /// If the loop is a simple loop, this property
        /// will be populated with the walk results.
        /// </summary>
        public WalkResults WalkResults { get; private set; }
        
        public Loop(int index, IEnumerable<LanguageInstruction> instructions, List<Loop> nestedLoops)
        {
            Index = index;
            Instructions = instructions.ToArray();
            NestedLoops = nestedLoops;

            if (IsSimple())
            {
                WalkResults = Walk();
            }
        }

        /// <summary>
        /// Constructs a Loop given a set of instructions and 
        /// an offset of the position of where the loop starts
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="offset">The index of the StartLoop instruction</param>
        /// <returns></returns>
        public static Loop Construct(LanguageInstruction[] instructions, int offset)
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
        public IEnumerable<LanguageInstruction> GetInstructionsSkippingNestedLoops()
        {
            for (int i = 0; i < Instructions.Length; i++)
            {
                var instruction = Instructions[i];
                if (!(instruction == LanguageInstruction.StartLoop || instruction == LanguageInstruction.EndLoop))
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
                if (Instructions[0] == LanguageInstruction.Dec || Instructions[0] == LanguageInstruction.Inc)
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
        /// The walk skips nested loops.
        /// 
        /// So if this loop is [>+++[>++]-] 
        /// the walk results will be:
        /// {{1:2}} 
        /// 
        /// The nested loop was skipped.
        /// </summary>
        /// <returns></returns>
        public WalkResults Walk()
        {
            int ptrIndex = 0;
            var domain = new SortedDictionary<int, int>();

            for (int i = 0; i < Instructions.Length; i++)
            {
                var instruction = Instructions[i];
                switch (instruction)
                {
                    case LanguageInstruction.IncPtr: ptrIndex++; break;
                    case LanguageInstruction.DecPtr: ptrIndex--; break;
                    case LanguageInstruction.Inc: AddOperationToDomain(domain, ptrIndex); break;
                    case LanguageInstruction.Dec: AddOperationToDomain(domain, ptrIndex, -1); break;
                    case LanguageInstruction.StartLoop: i = GetNextClosingLoopIndex(i).Value; break;
                }
            }

            return new WalkResults(domain, ptrIndex, Instructions.Count());
        }

        private int AddOperationToDomain(SortedDictionary<int, int> domain, int index, int step = 1)
        {
            if (domain.ContainsKey(index))
            {
                domain[index] += step;
                return domain[index];
            }

            domain.Add(index, step);
            return step;
        }

        /// <summary>
        /// Given a set of instructions and an offset of where the loop starts,
        /// this method returns the operations that the loop contains
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="offset">The index of the StartLoop instruction</param>
        /// <returns></returns>
        private static LanguageInstruction[] GetLoopInstructions(LanguageInstruction[] instructions, int offset)
        {
            var closingEndLoopIndex = GetNextClosingLoopIndex(instructions, offset).Value;
            return instructions.Skip(offset + 1).Take(closingEndLoopIndex - offset - 1).ToArray();
        }

        /// <summary>
        /// Given a set of instructions, this method returns a collection of 
        /// nested loops contained in the set.
        /// </summary>
        /// <returns></returns>
        private static List<Loop> GetNestedLoops(LanguageInstruction[] instructions)
        {
            var nestedLoops = new List<Loop>();
            var loopInstructions = instructions;
            var containsNestedLoops = loopInstructions.Any(li => li == LanguageInstruction.StartLoop);
            if (!containsNestedLoops)
            {
                return nestedLoops;
            }

            for (int i = 0; i < instructions.Length; i++)
            {
                var ins = instructions[i];
                if (!(ins == LanguageInstruction.StartLoop || ins == LanguageInstruction.EndLoop))
                {
                    continue;
                }

                if (ins == LanguageInstruction.StartLoop)
                {
                    var lInstructions = GetLoopInstructions(instructions, i);
                    var loop = new Loop(i, lInstructions, GetNestedLoops(lInstructions));
                    nestedLoops.Add(loop);

                    i = GetNextClosingLoopIndex(instructions, i).Value;
                }
            }

            return nestedLoops;
        }

        private static int? GetNextClosingLoopIndex(LanguageInstruction[] instructions, int index)
        {
            int stack = 0;

            for (int i = index + 1; i < instructions.Length; i++)
            {
                if (instructions[i] == LanguageInstruction.StartLoop)
                {
                    stack += 1;
                }

                if (instructions[i] == LanguageInstruction.EndLoop)
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

            bool containsIO = instructionsWithoutNestedLoops.Any(i => i == LanguageInstruction.Input || i == LanguageInstruction.Output);

            if (containsIO)
            {
                return false;
            }


            int totalIncPtrs = instructionsWithoutNestedLoops.Count(i => i == LanguageInstruction.IncPtr),
                totalDecPtrs = instructionsWithoutNestedLoops.Count(i => i == LanguageInstruction.DecPtr);

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
