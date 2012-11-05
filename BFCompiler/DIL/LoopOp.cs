using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.DIL
{
    /// <summary>
    /// StartLoop, EndLoop
    /// </summary>
    internal class LoopOp : DILOperation
    {
        private IEnumerable<DILOperation> Operations { get; set; }

        public LoopOp(int offset, IEnumerable<DILOperation> operations):base(offset)
        {
            Operations = operations;
        }

        //public bool IsSimpleLoop()
        //{
        //    return default(bool);
        //}
    }
}
