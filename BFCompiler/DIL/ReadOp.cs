
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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
