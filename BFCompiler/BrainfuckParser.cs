using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    class BrainfuckParser : Parser
    {
        public BrainfuckParser() : base(new[] { ">", "<", "+", "-", ".", ",", "[", "]" }, 1)
        {
        }
    }
}