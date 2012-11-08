
using System;
using System.Reflection.Emit;

namespace YABFcompiler.DIL
{
    using System.Collections.Generic;

    class LoopOp:DILInstruction
    {
        public Loop Loop { get; private set; }
        public DILOperationSet Instructions { get; private set; } 

        public LoopOp(Loop loop)
        {
            Loop = loop;
            Instructions = DILOperationSet.Generate(loop.Instructions);
        }

        public List<DILInstruction> Unroll()
        {
            var unrolled = new List<DILInstruction>();
            foreach (var cell in Loop.WalkResults.Domain)
            {
                if (cell.Key == 0)
                {
                    continue;
                }

                unrolled.Add(new MultiplicationMemoryOp(cell.Key, cell.Value));
            }

            if(Loop.WalkResults.Domain.ContainsKey(0))
            {
                unrolled.Add(new AssignOp(0, 0));
            }

            return unrolled;
        }
 
        public void Emit(ILGenerator ilg,LocalBuilder array, LocalBuilder ptr)
        {
            var labels = EmitStartLoop(ilg);
            foreach (var instruction in Instructions)
            {
                instruction.Emit(ilg, array, ptr);
            }

            EmitEndLoop(ilg, array, ptr, labels.Item2, labels.Item1);
        }

        private Tuple<Label,Label> EmitStartLoop(ILGenerator ilg)
        {
            var L_0008 = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br, L_0008);
            //loopStack.Push(L_0008);

            var L_0004 = ilg.DefineLabel();
            ilg.MarkLabel(L_0004);
            //loopStack.Push(L_0004);

            return new Tuple<Label, Label>(L_0008, L_0004);
        }

        private void EmitEndLoop(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, Label go, Label mark)
        {
            //Label go = loopStack.Pop(), mark = loopStack.Pop();
            ilg.MarkLabel(mark);
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.Emit(OpCodes.Brtrue, go);
        }
    }
}
