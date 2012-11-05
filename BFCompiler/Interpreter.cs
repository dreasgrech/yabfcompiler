using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler
{
    internal class Interpreter
    {
        private readonly Parser parser;
 
        public Interpreter(Parser parser)
        {
            this.parser = parser;
        }

        public void Run(string source)
        {
            var instructions = parser.GenerateDIL(source);

            var array = new char[0x493e0];
            int ptr = 0;
            foreach (var instruction in instructions)
            {
                switch (instruction)
                {
                    //case DILInstruction.Inc:array[ptr]
                }
            }

            throw new NotImplementedException("Still working on this one");
        }
    }
}
