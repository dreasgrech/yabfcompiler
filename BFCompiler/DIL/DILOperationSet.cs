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

        public WalkResults Walk()
        {
            int ptrIndex = 0;
            var domain = new SortedDictionary<int, int>();
            var miscOperations = new SortedDictionary<int, DILInstruction>();
            for (int i = 0; i < Count; i++)
            {
                var instruction = this[i];
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
                    var assign = (AssignOp) instruction;
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
                    var loop = (LoopOp) instruction;
                }
            }

            return new WalkResults(domain, ptrIndex, this.Count, miscOperations);
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
            bool wasOptimized = false;
            var newSet = new DILOperationSet();

            if (LoopExpansion(ref optimized))
            {
                return true;
            }



            var walk = Walk();
            foreach (var cell in walk.Domain)
            {
                if (cell.Value != 0)
                {
                    newSet.Add(new AdditionMemoryOp(cell.Key, cell.Value));
                }
            }

            if (walk.EndPtrPosition != 0)
            {
                newSet.Add(new PtrOp(walk.EndPtrPosition));
            }

            foreach (var miscOperation in walk.MiscOperations)
            {
                newSet.Add(miscOperation.Value);
            }

            if (wasOptimized = !newSet.AreDILOperationSetsIdentical(optimized))
            {
                optimized = newSet;
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

        private bool AreDILOperationSetsIdentical(DILOperationSet otherSet)
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
            var wasLoopExpanded = false;
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
                    wasLoopExpanded = true;
                    operations.RemoveAt(i); // remove the loop
                    operations.InsertRange(i, unrolled);
                    i += unrolled.Count;
                }

            }

            return wasLoopExpanded;
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
                    arr[i] = null;
                    var ptrOp = (PtrOp) operation;
                    ptr += ptrOp.Delta;
                    didAnysubstitutions = true;
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
