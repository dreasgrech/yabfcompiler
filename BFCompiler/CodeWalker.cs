using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YABFcompiler.DIL;

namespace YABFcompiler
{
    /*
     * this class is still very heavily under construction
     */
    class CodeWalker
    {
        private readonly DILInstruction[] instructions;

        public CodeWalker(DILInstruction[] operations)
        {
            instructions = operations;
        }

        public IEnumerable<DILOperation> Parse()
        {
            var operations = new List<DILOperation>();
            var domain = new SortedDictionary<int, int>(); // cell index (offset), value
            int domainPtr = 0, programPtr = 0;

            for (int i = 0; i < instructions.Length; i++)
            {
                var instruction = instructions[i];
                if (instruction == DILInstruction.Inc || instruction == DILInstruction.Dec)
                {
                    switch (instruction)
                    {
                        case DILInstruction.Inc:
                            {
                                AddOperationToDomain(domain, domainPtr, 1);
                                operations.Add(new MemOp(0, 1));
                            }
                            break;
                        case DILInstruction.Dec: AddOperationToDomain(domain, domainPtr, -1); break;
                    }
                }
            }

            return null;
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
    }
}
