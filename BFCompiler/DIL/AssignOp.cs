
namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    class AssignOp : DILInstruction
    {
        public int Offset { get; set; }
        public int Value { get; set; }
        public ConstantValue Constant { get; set; }

        public AssignOp(int offset, int value, ConstantValue constant)
        {
            Offset = offset;
            Value = value;
            Constant = constant;
        }

        public AssignOp(int offset, int value) : this(offset, value, null)
        {
            
        }
    }
}
