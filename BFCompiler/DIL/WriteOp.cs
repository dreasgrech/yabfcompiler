
using System.Diagnostics;

namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [DebuggerDisplay("Output")]
    class WriteOp : DILInstruction
    {
        public ConstantValue Constant { get; private set; }

        public WriteOp()
        {
            
        }
        public WriteOp(ConstantValue constant)
        {
            Constant = constant;
        }
    }
}
