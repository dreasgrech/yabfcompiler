using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace YABFcompiler.DIL
{
    class DILOperationSet:List<DILInstruction>
    {
        public DILOperationSet(){}
        public DILOperationSet(IEnumerable<DILInstruction> instructions)
        {
            AddRange(instructions);
        }

        public new void Add(DILInstruction instruction)
        {
            base.Add(instruction);
        }

        private int? GetNextInstructionIndex<T>(int index) where T:DILInstruction
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
                    var cellValue = domain[ptrIndex];
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

                if (instruction is LoopOp)
                {
                    var loop = (LoopOp)instruction;
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

        public bool Optimize(ref DILOperationSet optimized)
        {
            var newSet = new DILOperationSet();

            while (LoopExpansion(ref optimized)) // Expand all loops which can expaned
            {}

            optimized.RemoveAll(i => i == null);

            bool wasOptimized = false;
            for (int i = 0; i < optimized.Count; i++)
            {
                var instruction = optimized[i];

                //if (i == 0)
                //{
                //    var assOp = instruction as AssignOp;
                //    if (assOp != null && assOp.Offset == 0)
                //    {
                //        if (assOp.Value == 0)
                //        {
                //            optimized[0] = null;
                //            continue;
                //        }
                //    }

                //    var addOpp = instruction as AdditionMemoryOp;
                //    if (addOpp != null)
                //    {
                //        optimized[0] = new AssignOp(addOpp.Offset, addOpp.Scalar);
                //    }
                //    //if (instruction is add)
                //}

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
                    if (addOpp != null && addOpp.Offset == 0)
                    {
                        if (addOpp.Scalar == 0) // Trying to add +0 ?
                        {
                            optimized[0] = null;
                        }
                        else
                        {
                            optimized[0] = new AssignOp(addOpp.Offset, addOpp.Scalar);
                        }
                    }
                }

                var removed = optimized.RemoveAll(o => o == null);
                if (i != 0)
                {
                    i -= removed;
                }

                var walk = Walk(i);
                var tempSet = new DILOperationSet();
                foreach (var cell in walk.Domain)
                {
                    if (cell.Value != 0)
                    {
                    tempSet.Add(new AdditionMemoryOp(cell.Key, cell.Value));
                    }
                    //else
                    //{
                    //    newSet.Add(new AssignOp(cell.Key, cell.Value));
                    //}
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
                    i += newSet.Count - 1;
                    continue;
                } 

                i += newSet.Count - 1;
            }

            if (wasOptimized)
            {
                return true;
            }

            if (CanWeSubstituteConstants())
            {
                if (SubstituteConstants(ref optimized))
                {
                    return true;
                }
            }



            //if (IsSimple()) // Contains only additionmemoryops and ptrops
            //{

            //}



            return wasOptimized;
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
        public bool LoopExpansion(ref DILOperationSet operations)
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
                    //arr[i] = null;
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

        private bool IsSimple()
        {
            return this.All(i => i is AdditionMemoryOp || i is PtrOp || i is WriteOp || i is ReadOp);
        }
    }
}
