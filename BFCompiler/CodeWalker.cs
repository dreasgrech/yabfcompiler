﻿
namespace YABFcompiler
{
    using System.Collections.Generic;
    using DIL;

    class CodeWalker
    {
        /// <summary>
        /// Walks through the entire set of operations that it's provided.
        /// Loops are skipped.
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="index"></param>
        /// <param name="stopAtLoop"></param>
        /// <returns></returns>
        public WalkResults Walk(DILOperationSet operations)
        {
            int ptrIndex = 0;
            var domain = new SortedDictionary<int, int>();
            var miscOperations = new SortedDictionary<int, DILInstruction>();

            foreach (var instruction in operations)
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

            return new WalkResults(domain, ptrIndex, operations.Count, miscOperations);
        }

        private int? GetNextInstructionIndex<T>(DILOperationSet operations, int index) where T : DILInstruction
        {
            for (int i = index; i < operations.Count; i++)
            {
                if (operations[i].GetType() == typeof(T))
                {
                    return i;
                }
            }

            return null;
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
    }
}
