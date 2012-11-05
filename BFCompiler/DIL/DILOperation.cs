using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.DIL
{
    abstract class DILOperation
    {
        public int Offset { get; set; }

        protected DILOperation(int offset)
        {
            Offset = offset;
        }
    }
}
