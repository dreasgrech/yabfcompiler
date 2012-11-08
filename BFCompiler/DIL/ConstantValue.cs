using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.DIL
{
    class ConstantValue
    {
        public int Value { get; private set; }
        public ConstantValue(int constant)
        {
            Value = constant;
        }
    }
}
