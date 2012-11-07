
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class AdditionMemoryOp : DILInstruction
    {
        public int Offset { get; set; }
        public int Scalar { get; set; }

        public AdditionMemoryOp(int offset, int scalar)
        {
            Offset = offset;
            Scalar = scalar;
        }
    }
}
