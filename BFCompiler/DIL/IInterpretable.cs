using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YABFcompiler.DIL
{
    interface IInterpretable
    {
        void Interpret(byte[] domain, ref int ptr);
    }
}
