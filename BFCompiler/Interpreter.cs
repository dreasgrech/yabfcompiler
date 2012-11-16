
namespace YABFcompiler
{
    using System;
    using DIL;

    internal class Interpreter
    {
        public byte[] Domain { get; private set; }
        public int Ptr { get; private set; }

        public Interpreter(int domainSize)
        {
            Domain = new byte[domainSize];
        }

        public void Run(DILOperationSet operations)
        {
            int ptr = Ptr; // since you can't pass a Property by reference...
            foreach (var instruction in operations)
            {
                var interpretableInstruction = instruction as IInterpretable;
                if (interpretableInstruction != null)
                {
                    interpretableInstruction.Interpret(Domain, ref ptr);
                    Ptr = ptr;
                } else
                {
                    throw new Exception("Why isn't this instruction interpretable?");
                }
            }
        }
    }
}
