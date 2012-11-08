﻿
using System.Diagnostics;

namespace YABFcompiler.DIL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [DebuggerDisplay("Ptr => Delta = {Delta}")]
    class PtrOp : DILInstruction
    {
        public int Delta { get; set; }

        public PtrOp(int delta)
        {
            Delta = delta;
        }
    }
}