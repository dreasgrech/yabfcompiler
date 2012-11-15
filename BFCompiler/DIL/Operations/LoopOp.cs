
namespace YABFcompiler.DIL.Operations
{
    using System.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;

    [DebuggerDisplay("Loop => Simple: {Simple}")]
    class LoopOp:DILInstruction
    {
        public DILOperationSet Instructions { get; set; }
        public List<LoopOp> NestedLoops { get;private set;}

        public LoopOp(Loop loop):this(new DILOperationSet(loop.Instructions))
        {
            
        }

        public LoopOp(DILOperationSet instructions)
        {
            Instructions = instructions;
            NestedLoops = Instructions.OfType<LoopOp>().ToList();
        }

        public LoopUnrollingResults Unroll()
        {
            var unrolled = new DILOperationSet();
            //if (IsClearanceLoop())
            //{
            //    unrolled.Add(new AssignOp(0, 0));
            //    return new LoopUnrollingResults(unrolled, true);
            //}

            var withUnrolledNestLoops = new DILOperationSet();
            foreach (var instruction in Instructions)
            {
                if (instruction is LoopOp)
                {
                    var nestedLoop = instruction as LoopOp;
                    var ur = nestedLoop.Unroll();
                    if (ur.WasLoopUnrolled)
                    {
                        withUnrolledNestLoops.AddRange(ur.UnrolledInstructions);
                    } else
                    {
                        withUnrolledNestLoops.Add(new LoopOp(ur.UnrolledInstructions));
                    }
                }
                else
                {
                    withUnrolledNestLoops.Add(instruction);
                }
            }

            if (IsSimple(withUnrolledNestLoops))
            {
                var walk = new CodeWalker().Walk(withUnrolledNestLoops);
                if (walk.Domain.ContainsKey(0) && walk.Domain[0] == -1)
                {
                    foreach (var cell in walk.Domain)
                    {
                        if (cell.Key == 0)
                        {
                            continue;
                        }

                        //// If the scalar value of the multiplication operation is 0,
                        //// then simply assign 0 to the cell because n * 0 = 0.
                        //if (cell.Value == 0)
                        //{
                        //    unrolled.Add(new AssignOp(cell.Key, 0));
                        //}
                        //else
                        //{
                            unrolled.Add(new MultiplicationMemoryOp(cell.Key, cell.Value));
                        //}
                    }

                    // If it's a simple loop, then the cell position of the loop should always be assigned a 0 since that's when the loop stops.
                    if (walk.Domain.ContainsKey(0))
                    {
                        unrolled.Add(new AssignOp(0, 0));
                    }

                    return new LoopUnrollingResults(unrolled, true);
                } else
                {
                    return new LoopUnrollingResults(withUnrolledNestLoops, false);
                    
                }
            }

            return new LoopUnrollingResults(withUnrolledNestLoops, false);
        }

        /// <summary>
        /// I'm only using this method for debugging purposes
        /// </summary>
        public bool Simple{get
        {
            return IsSimple(Instructions);            
        }}

        /// <summary>
        /// A simple loop is a loop which, when ignoring nested loops, the pointer returns to the initial position of the loop after execution
        /// 
        /// A nested loop also doesn't contain any IO.
        /// 
        /// This method needs to be changed to an instance method
        /// </summary>
        /// <param name="operations"></param>
        /// <returns></returns>
        public static bool IsSimple(DILOperationSet operations)
        {
            return
                new CodeWalker().Walk(operations).EndPtrPosition == 0
                && !ContainsIO(operations);
        }

        /// <summary>
        /// This method needs to be changed to an instance method
        /// </summary>
        /// <param name="operations"></param>
        /// <returns></returns>
        public static bool ContainsIO(DILOperationSet operations)
        {
            return operations.Any(o => o is WriteOp || o is ReadOp);
        }

        /// <summary>
        /// Returns true if a clearance pattern is detected with this loop
        /// 
        /// The following patterns are currently detected:
        ///     [-], [+]
        /// </summary>
        /// <returns></returns>
        public bool IsClearanceLoop()
        {
            if (Instructions.Count == 1)
            {
                if (Instructions[0] is AdditionMemoryOp)
                {
                    return true;
                }
            }

            return false;
        }
 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="array"></param>
        /// <param name="ptr"></param>
        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
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

            var L_0004 = ilg.DefineLabel();
            ilg.MarkLabel(L_0004);

            return new Tuple<Label, Label>(L_0008, L_0004);
        }

        private void EmitEndLoop(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr, Label go, Label mark)
        {
            ilg.MarkLabel(mark);
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelem_U1);
            ilg.Emit(OpCodes.Brtrue, go);
        }
    }
}
