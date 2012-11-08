
using System.Diagnostics;

namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [DebuggerDisplay("Add => Offset: {Offset}, Scalar = {Scalar}")]
    class AdditionMemoryOp : DILInstruction
    {
        public int Offset { get; set; }
        public int Scalar { get; set; }
        public ConstantValue Constant { get; set; }

        public AdditionMemoryOp(int offset, int scalar, ConstantValue constant)
        {
            Offset = offset;
            Scalar = scalar;
            Constant = constant;
        }

        public AdditionMemoryOp(int offset, int scalar):this(offset, scalar, null)
        {
            
        }
    }
}
