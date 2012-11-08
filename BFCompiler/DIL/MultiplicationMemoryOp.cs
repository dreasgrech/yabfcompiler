
using System.Diagnostics;

namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [DebuggerDisplay("Mul => Offset: {Offset}, Scalar = {Scalar}")]
    class MultiplicationMemoryOp : DILInstruction
    {
        public int Offset { get; set; }
        public int Scalar { get; set; }

        public MultiplicationMemoryOp(int offset, int scalar)
        {
            Offset = offset;
            Scalar = scalar;
        }
    }
}
