

namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    [DebuggerDisplay("Input")]
    internal class ReadOp : DILInstruction
    {
        public ConstantValue Constant { get; private set; }

        public ReadOp()
        {

        }

        public ReadOp(ConstantValue constant)
        {
            Constant = constant;
        }
    }
}
