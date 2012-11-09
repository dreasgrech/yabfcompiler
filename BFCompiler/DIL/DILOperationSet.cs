﻿
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class DILOperationSet:List<DILInstruction>
    {
        public DILOperationSet(){}
        public DILOperationSet(IEnumerable<DILInstruction> instructions)
        {
            AddRange(instructions);
        }

        public static DILOperationSet Generate(LanguageInstruction[] languageInstructions)
        {
            var dilInstructions = new DILOperationSet();
            for (int i = 0; i < languageInstructions.Length; i++)
            {
                var instruction = languageInstructions[i];

                if ((instruction == LanguageInstruction.Inc || instruction == LanguageInstruction.Dec))
                {
                    dilInstructions.Add(new AdditionMemoryOp(0, instruction == LanguageInstruction.Inc ? 1 : -1));
                    continue;
                }

                if (instruction == LanguageInstruction.IncPtr || instruction == LanguageInstruction.DecPtr)
                {
                    dilInstructions.Add(new PtrOp(instruction == LanguageInstruction.IncPtr ? 1 : -1));
                    continue;
                }

                if (instruction == LanguageInstruction.StartLoop || instruction == LanguageInstruction.EndLoop)
                {
                    if (instruction == LanguageInstruction.StartLoop)
                    {
                        var loop = Loop.Construct(languageInstructions, i);
                        dilInstructions.Add(new LoopOp(loop));

                        i += loop.Instructions.Length + 1;
                    }

                    continue;
                }

                switch (instruction)
                {
                    case LanguageInstruction.Output: dilInstructions.Add(new WriteOp()); break;
                    case LanguageInstruction.Input: dilInstructions.Add(new ReadOp()); break;
                }
            }

            return dilInstructions;
        }

        public new void Add(DILInstruction instruction)
        {
            base.Add(instruction);
        }

        public bool Optimize(ref DILOperationSet optimized)
        {
            // First expand all the simple loops
            while (LoopExpansion(ref optimized)) { }

            while(CompactSameAdditionOperators(ref optimized)) {}

            optimized.RemoveAll(i => i == null);

            bool wasOptimized = false;
            do
            {
                var newSet = new DILOperationSet();

                for (int i = 0; i < optimized.Count; i++)
                {
                    var instruction = optimized[i];

                    if (optimized[0] is AdditionMemoryOp || optimized[0] is AssignOp)
                    {
                        var assOp = optimized[0] as AssignOp;
                        if (assOp != null && assOp.Offset == 0)
                        {
                            if (assOp.Value == 0)
                            {
                                optimized[0] = null;
                                continue;
                            }
                        }

                        var addOpp = optimized[0] as AdditionMemoryOp;
                        if (addOpp != null)
                        {
                            if (addOpp.Scalar == 0) // Trying to add +0 ?
                            {
                                optimized[0] = null;
                            }
                            else
                            {
                                //optimized[0] = new AssignOp(addOpp.Offset, addOpp.Scalar);
                            }
                        }
                    }

                    var removed = optimized.RemoveAll(o => o == null);
                    if (i != 0)
                    {
                        i -= removed;
                    }

                    // TODO: Continue working from here
                    // This problem here is with the CodeWalker Walk method
                    // because when I use the Walk method from this class, it works...

                    //var walk = new CodeWalker().Walk(this, i);
                    var walk = Walk(i);

                    var tempSet = new DILOperationSet();
                    foreach (var cell in walk.Domain)
                    {
                        if (cell.Value != 0)
                        {
                            tempSet.Add(new AdditionMemoryOp(cell.Key, cell.Value));
                        }
                    }

                    if (walk.EndPtrPosition != 0)
                    {
                        tempSet.Add(new PtrOp(walk.EndPtrPosition));
                    }

                    foreach (var miscOperation in walk.MiscOperations)
                    {
                        tempSet.Add(miscOperation.Value);
                    }

                    newSet.AddRange(tempSet);

                    var setThatWasOptimized = optimized.Skip(i).Take(walk.TotalInstructionsCovered - i).ToList();
                    if (!tempSet.AreDILOperationSetsIdentical(setThatWasOptimized))
                    {
                        optimized.RemoveRange(i, walk.TotalInstructionsCovered - i);
                        optimized.InsertRange(i, tempSet);

                        wasOptimized = true;
                        i += tempSet.Count - 1;
                        continue;
                    }

                    wasOptimized = false;
                }
            } while (wasOptimized);


            /* Begin: Constant Substitution */
            if (CanWeSubstituteConstants())
            {
                if (SubstituteConstants(ref optimized))
                {
                    return true;
                }
            }
            /* End: Constant Substitution */

            /* Begin: String walking */
            //var stringWalkResults = StringWalk();
            //if (stringWalkResults != null && stringWalkResults.Strings.Count > 0)
            //{
            //    optimized.Clear();
            //    var combine = String.Join("", stringWalkResults.Strings.Select(c => c.Value));
            //    optimized.Add(new WriteLiteralOp(combine));
            //    //foreach (var s in stringWalkResults.Strings)
            //    //{
            //    //    optimized.Add(new WriteLiteralOp(s.Value));
            //    //}
            //}
            /* End: String walking */

            return wasOptimized;
        }

        private bool CompactSameAdditionOperators(ref DILOperationSet operations)
        {
            for (int i = 0; i < operations.Count; i++)
            {
                var currentInstruction = operations[i] as AdditionMemoryOp;
                if (currentInstruction == null)
                {
                    continue;
                }

                int delta = currentInstruction.Scalar, totalOperationsCovered = 1;

                for (int j = i + 1; j < operations.Count; j++) // - i ?
                {
                    var instruction = operations[j] as AdditionMemoryOp;
                    if (instruction == null)
                    {
                        break;
                    }

                    if (instruction.Offset != currentInstruction.Offset)
                    {
                        break;
                    }

                    totalOperationsCovered++;
                    delta += instruction.Scalar;
                }

                if (totalOperationsCovered - 1 > 0)
                {
                    operations.RemoveRange(i, totalOperationsCovered);
                    operations.Insert(i, new AdditionMemoryOp(currentInstruction.Offset, delta));

                    return true;
                }
            }

            return false;
            //return new CompactOperationsResults(delta, totalOperationsCovered, firstAdditionInstruction.Offset);
        }

        private int? GetNextInstructionIndex<T>(int index) where T : DILInstruction
        {
            for (int i = index; i < Count; i++)
            {
                if (this[i].GetType() == typeof(T))
                {
                    return i;
                }
            }

            return null;
        }

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

                    continue;
                }

                //if (instruction is LoopOp)
                //{
                //    var loop = (LoopOp)instruction;
                //}
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

        private StringWalkResults StringWalk()
        {
            int ptr = 0;
            var domain = new Dictionary<int, char>();
            var strings = new Dictionary<int, string>();

            var containsOnlyAdditionAndWrites = this.All(i => i.GetType() == typeof (AdditionMemoryOp) || i.GetType() == typeof (WriteOp) || i.GetType() == typeof(PtrOp));
            if (containsOnlyAdditionAndWrites)
            {
                for (int i = 0; i < this.Count; i++)
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

                    var write = instruction as WriteOp;
                    if (write != null)
                    {
                        var key = write.Constant != null ? write.Constant.Value : ptr;

                        if (strings.ContainsKey(key))
                        {
                            strings[key] += domain[key].ToString();
                        } else
                        {
                            if (!domain.ContainsKey(key))
                            {
                                domain[key] = (char)0;
                            }

                            strings[key] = domain[key].ToString();
                        }
                    }
                }

                return new StringWalkResults(strings);
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
            for (int i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                if (!(operation is LoopOp))
                {
                    continue;
                }

                var loopOp = (LoopOp) operation;
                var unrolled = loopOp.Unroll();
                if (unrolled.Count > 0)
                {
                    operations.RemoveAt(i); // remove the loop
                    operations.InsertRange(i, unrolled);
                    return true; // One loop at a time
                }

            }

            return false;
        }

        /// <summary>
        /// Optimization #6:
        /// Constant substitution
        /// </summary>
        /// <param name="operations"></param>
        private bool SubstituteConstants(ref DILOperationSet operations)
        {
            var ptr = 0;
            var arr = operations.ToArray();
            var didAnysubstitutions = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var operation = arr[i];
                if (operation is AdditionMemoryOp)
                {
                    var add = (AdditionMemoryOp) operation;
                    if (((AdditionMemoryOp) arr[i]).Constant == null)
                    {
                        arr[i] = new AdditionMemoryOp(ptr + add.Offset, add.Scalar, new ConstantValue(ptr + add.Offset));
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
                    if (((WriteOp)arr[i]).Constant == null)
                    {
                        arr[i] = new WriteOp(new ConstantValue(ptr));
                        didAnysubstitutions = true;
                    }
                }
                else if (operation is ReadOp)
                {
                    if (((ReadOp)arr[i]).Constant == null)
                    {
                        arr[i] = new ReadOp(new ConstantValue(ptr));
                        didAnysubstitutions = true;
                    }
                }
            }

            var list = arr.ToList();
            list.RemoveAll(l => l == null);

            operations = new DILOperationSet(list);

            return didAnysubstitutions;
        }

        private bool CanWeSubstituteConstants()
        {
            return !this.Any(i => i is LoopOp);
        }
    }
}
