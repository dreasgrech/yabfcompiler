using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.DIL
{
    /// <summary>
    /// Inc, Dec
    /// </summary>
    class MemOp : DILOperation
    {
        public int Delta { get; set; }

        public MemOp(int offset, int delta):base(offset)
        {
            Offset = offset;
            Delta = delta;
        }
    }
}
