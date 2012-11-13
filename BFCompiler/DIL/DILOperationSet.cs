
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class DILOperationSet:List<DILInstruction>
    {
        public bool WereConstantsSubstituted { get; private set; }

        public DILOperationSet(){}
        public DILOperationSet(IEnumerable<DILInstruction> instructions)
        {
            AddRange(instructions);
        }

        public DILOperationSet(LanguageInstruction[] languageInstructions)
        {
            for (int i = 0; i < languageInstructions.Length; i++)
            {
                var instruction = languageInstructions[i];

                if ((instruction == LanguageInstruction.Inc || instruction == LanguageInstruction.Dec))
                {
                    Add(new AdditionMemoryOp(0, instruction == LanguageInstruction.Inc ? 1 : -1));
                    continue;
                }

                if (instruction == LanguageInstruction.IncPtr || instruction == LanguageInstruction.DecPtr)
                {
                    Add(new PtrOp(instruction == LanguageInstruction.IncPtr ? 1 : -1));
                    continue;
                }

                if (instruction == LanguageInstruction.StartLoop || instruction == LanguageInstruction.EndLoop)
                {
                    if (instruction == LanguageInstruction.StartLoop)
                    {
                        var loop = Loop.Construct(languageInstructions, i);
                        Add(new LoopOp(loop));

                        i += loop.Instructions.Length + 1;
                    }

                    continue;
                }

                switch (instruction)
                {
                    case LanguageInstruction.Output: Add(new WriteOp(0, 1)); break;
                    case LanguageInstruction.Input: Add(new ReadOp(0, 1)); break;
                }
            }
        }

        public new void Add(DILInstruction instruction)
        {
            base.Add(instruction);
        }

        public bool Optimize(ref DILOperationSet optimized)
        {

            while (CompactRepeatableOperations(ref optimized))
            {
            }

            // expand all the simple loops
            while (LoopExpansion(ref optimized))
            {
                return true; // After some loops have been expanded, there might be new repeatable operations which need to be compacted.
            }

            optimized.RemoveAll(i => i == null);

            //if (!WereConstantsSubstituted)
            //{
            //    int currentIndex = 0, nextIOOperationIndex, currentPtrIndex = 0;
            //    while (
            //        currentIndex <= optimized.Count &&
            //        (nextIOOperationIndex = optimized.FindIndex(currentIndex,
            //            i => i is WriteOp || i is ReadOp /*|| i is AssignOp*/)) != -1)
            //    {
            //        var subOperationSet = new DILOperationSet(optimized.Skip(currentIndex).Take(nextIOOperationIndex - currentIndex));

            //        //foreach (var subOperation in subOperationSet)
            //        //{
            //        //    var offsettable = subOperation as IOffsettable;
            //        //    if (offsettable != null)
            //        //    {
            //        //        offsettable.Offset += currentPtrIndex;
            //        //    }
            //        //}

            //        var walk = new CodeWalker().Walk(optimized, nextIOOperationIndex);
            //        //var walk = Walk(i);

            //        var tempSet = new DILOperationSet();
            //        foreach (var cell in walk.Domain)
            //        {
            //            if (cell.Value != 0)
            //            {
            //                tempSet.Add(new AdditionMemoryOp(cell.Key, cell.Value));
            //            }
            //        }

            //        if (walk.EndPtrPosition != 0)
            //        {
            //            tempSet.Add(new PtrOp(walk.EndPtrPosition));
            //        }

            //        //foreach (var miscOperation in walk.MiscOperations)
            //        //{
            //        //    tempSet.Add(miscOperation.Value);
            //        //}

            //        currentPtrIndex += walk.EndPtrPosition;

            //        if (nextIOOperationIndex - currentIndex < optimized.Count)
            //        {
            //            var op = optimized[nextIOOperationIndex - currentIndex];
            //            if (op is ReadOp || op is WriteOp)
            //            {
            //                ((IOffsettable)op).Offset = currentPtrIndex;
            //            }

            //        }

            //        optimized.RemoveRange(currentIndex, nextIOOperationIndex - currentIndex);
            //        optimized.InsertRange(currentIndex, tempSet);

            //        currentIndex += tempSet.Count + 1; // + 1 to skip the IO operation
            //    }
            //}

            /* Constant Substitution */
            //if (CanWeSubstituteConstants())
            //{
            //    if (SubstituteConstants(ref optimized))
            //    {
            //        return true;
            //    }
            //}

            /* String walking */
            //var stringWalkResults = StringWalk();
            //if (stringWalkResults != null && stringWalkResults.Strings.Count > 0)
            //{
            //    optimized.Clear();
            //    var combine = String.Join("", stringWalkResults.Strings);
            //    optimized.Add(new WriteLiteralOp(combine));

            //    return true;
            //}

            return false;
        }

        private bool CompactRepeatableOperations(ref DILOperationSet operations)
        {
            var wasOptimized = false;
            for (int i = 0; i < operations.Count; i++)
            {
                var loopInstruction = operations[i] as LoopOp;
                if (loopInstruction != null)
                {
                    var loopInstructions = loopInstruction.Instructions;
                    CompactRepeatableOperations(ref loopInstructions);
                    loopInstruction.Instructions = loopInstructions;
                    continue;
                }

                var repeatable = operations[i] as IRepeatable;
                if (repeatable != null)
                {
                    if (repeatable.Repeat(operations, i))
                    {
                        wasOptimized = true;
                    }
                }
            }

            return wasOptimized;
        }

        private int? GetNextInstructionIndex<T>(int index) where T : DILInstruction
        {
            for (int i = index; i < Count; i++)
            {
                if (this[i] is T)
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// This method needs to be removed, and CodeWalker.Walk should be used instead.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private WalkResults Walk(int index)
        {
            int ptrIndex = 0;
            var domain = new SortedDictionary<int, int>();
            var miscOperations = new SortedDictionary<int, DILInstruction>();

            var end = Count;
            int whereToStop = Math.Min(
                1 + Math.Min((GetNextInstructionIndex<ReadOp>(index) ?? end), GetNextInstructionIndex<WriteOp>(index) ?? end)
                , GetNextInstructionIndex<LoopOp>(index) ?? end);

            var ins = this.Skip(index).Take(whereToStop - index).ToArray();

            foreach (var instruction in ins)
            {
                if (instruction is AdditionMemoryOp)
                {
                    var add = ((AdditionMemoryOp)instruction);
                    AddOperationToDomain(domain, ptrIndex + add.Offset, add.Scalar);
                    continue;
                }

                if (instruction is MultiplicationMemoryOp)
                {
                    var mul = ((MultiplicationMemoryOp)instruction);
                    var cellValue = domain.ContainsKey(ptrIndex) ? domain[ptrIndex] : 0;
                    MultiplyOperationToDomain(domain, ptrIndex + mul.Offset, cellValue * mul.Scalar);
                    continue;
                }

                if (instruction is AssignOp)
                {
                    var assign = (AssignOp)instruction;
                    AssignOperationToDomain(domain, ptrIndex + assign.Offset, assign.Value);
                }

                if (instruction is PtrOp)
                {
                    var ptr = ((PtrOp)instruction);
                    ptrIndex += ptr.Delta;
                    continue;
                }

                if (instruction is WriteOp || instruction is ReadOp)
                {
                    if (!miscOperations.ContainsKey(ptrIndex))
                    {
                        miscOperations.Add(ptrIndex, instruction);
                    }
                }
            }

            return new WalkResults(domain, ptrIndex, whereToStop, miscOperations);
        }

        private static int AddOperationToDomain(SortedDictionary<int, int> domain, int index, int step = 1)
        {
            if (domain.ContainsKey(index))
            {
                domain[index] += step;
                return domain[index];
            }

            domain.Add(index, step);
            return step;
        }

        private static int MultiplyOperationToDomain(SortedDictionary<int, int> domain, int index, int step = 1)
        {
            if (domain.ContainsKey(index))
            {
                domain[index] *= step;
                return domain[index];
            }

            domain.Add(index, step); // TODO: should step be 0 here because of 0 * step ?
            return step;
        }

        private static int AssignOperationToDomain(SortedDictionary<int, int> domain, int index, int value = 0)
        {
            if (domain.ContainsKey(index))
            {
                domain[index] = value;
                return domain[index];
            }

            domain.Add(index, value);
            return value;
        }

        /// <summary>
        /// TODO: This method currently does not compute MultiplicationMemoryOps! :(
        ///       I need to decide whether I should support PtrOps in this method.
        ///       I think it would be best if not to because 
        /// 
        /// </summary>
        /// <returns></returns>
        private StringWalkResults StringWalk()
        {
            int ptr = 0;
            var domain = new Dictionary<int, char>();
            var strings = new List<string>();

            var canWeWalk = this.All(i =>
                i is AdditionMemoryOp ||
                // i is MultiplicationMemoryOp ||
                i is WriteOp || 
                i is PtrOp ||
                i is AssignOp);

            if (canWeWalk)
            {
                for (int i = 0; i < Count; i++)
                {
                    var instruction = this[i];

                    var ptrOp = instruction as PtrOp;
                    if (ptrOp != null)
                    {
                        ptr += ptrOp.Delta;
                        continue;
                    }

                    var add = instruction as AdditionMemoryOp;
                    if (add != null)
                    {
                        if (domain.ContainsKey(add.Offset))
                        {
                            domain[add.Offset] += (char)add.Scalar;
                        } else
                        {
                            domain[add.Offset] = (char)add.Scalar;
                        }
                        continue;
                    }

                    var mul = instruction as MultiplicationMemoryOp;
                    if (mul != null)
                    {
                        // multiplication operation in here
                    }

                    var write = instruction as WriteOp;
                    if (write != null)
                    {
                       var key = write.Constant != null ? write.Constant.Value : write.Offset + ptr;

                        strings.Add(new string(domain[key], write.Repeated));

                        continue;
                    }

                    var ass = instruction as AssignOp;
                    if (ass != null)
                    {
                        domain[ptr + ass.Offset] = (char)ass.Value;
                    }
                }

                return new StringWalkResults(strings, domain);
            }

            return null;
        }

        private bool AreDILOperationSetsIdentical(List<DILInstruction> otherSet)
        {
            if (Count != otherSet.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (this[i].GetType() != otherSet[i].GetType())
                {
                    return false;
                }
            }

            return true;
        }

        private bool LoopExpansion(ref DILOperationSet operations)
        {
            if (!operations.ContainsLoops())
            {
                return false;
            }

            for (int i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                if (!(operation is LoopOp))
                {
                    continue;
                }

                var loopOp = (LoopOp) operation;
                var unrolled = loopOp.Unroll();
                if (unrolled.WasLoopUnrolled)
                {
                    operations.RemoveAt(i); // remove the loop
                    operations.InsertRange(i, unrolled.UnrolledInstructions);
                    return true; // One loop at a time
                }

                operations.RemoveAt(i);
                operations.Insert(i, new LoopOp(unrolled.UnrolledInstructions));
            }

            return false;
        }

        /// <summary>
        /// Optimization #6:
        /// Constant substitution
        /// 
        /// TODO: This method has some inconsistencies when setting the offset with the new memory operations.
        ///       For some memory operations, I'm using "new {Op}(ptr + {op}.Offset)" 
        ///       and for others I'm just using "new {Op}({op}.Offset)".  
        ///       I'm pretty sure this inconsistency is causing some problems.
        /// </summary>
        /// <param name="operations"></param>
        private bool SubstituteConstants(ref DILOperationSet operations)
        {
            if (WereConstantsSubstituted)
            {
                return false;
            }

            var ptr = 0;
            var arr = operations.ToArray();
            var didAnysubstitutions = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var operation = arr[i];
                if (operation is AdditionMemoryOp)
                {
                    var add = (AdditionMemoryOp) operation;
                    if (add.Constant == null)
                    {
                        arr[i] = new AdditionMemoryOp(ptr + add.Offset, add.Scalar, new ConstantValue(ptr + add.Offset));
                        didAnysubstitutions = true;
                    }
                }
                else if (operation is MultiplicationMemoryOp)
                {
                    var mul = (MultiplicationMemoryOp)operation;
                    if (mul.Constant == null)
                    {
                        arr[i] = new MultiplicationMemoryOp(ptr + mul.Offset, mul.Scalar, new ConstantValue(ptr + mul.Offset), new ConstantValue(ptr));
                    }
                }
                else if (operation is AssignOp)
                {
                    var ass = arr[i] as AssignOp;
                    if (ass.Constant == null)
                    {
                        arr[i] = new AssignOp(ptr + ass.Offset, ass.Value, new ConstantValue(ptr + ass.Offset));
                        didAnysubstitutions = true;
                    }
                }
                else if (operation is PtrOp)
                {
                    arr[i] = null; // if we're using constants, then we don't need pointer movements
                    var ptrOp = (PtrOp) operation;
                    ptr += ptrOp.Delta;
                    //didAnysubstitutions = true;
                }
                else if (operation is WriteOp)
                {
                    var write = ((WriteOp) arr[i]);
                    if (write.Constant == null)
                    {
                        arr[i] = new WriteOp(write.Offset, write.Repeated, new ConstantValue(ptr));
                        didAnysubstitutions = true;
                    }
                }
                else if (operation is ReadOp)
                {
                    var read = ((ReadOp) arr[i]);
                    if (read.Constant == null)
                    {
                        arr[i] = new ReadOp(read.Offset, read.Repeated, new ConstantValue(ptr));
                        didAnysubstitutions = true;
                    }
                }
            }

            var list = arr.ToList();
            list.RemoveAll(l => l == null);

            operations = new DILOperationSet(list);

            operations.WereConstantsSubstituted = true;

            return didAnysubstitutions;
        }

        private bool CanWeSubstituteConstants()
        {
            return !ContainsLoops();
        }

        public bool ContainsLoops()
        {
            return this.Any(i => i is LoopOp);
        }

        public bool ContainsPointerOperations()
        {
            return this.Any(i => i is PtrOp);
        }
    }
}
