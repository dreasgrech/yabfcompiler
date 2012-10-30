
namespace YABFcompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using Exceptions;
    using ILConstructs;

    /*
     * Optimization #1:
     *  Loops which could never be entered are ignored.
     *  This can happen when either:
     *      1) A loop starts immediately after another loop or
     *      2) The loop is at the beginning of the program.
     *  
     * Optimization #2:
     *  Sequences of Input and Output are grouped in a for-loop
     *  
     * NOTE: I'm not sure how beneficial this optimization is because although it can reduce the 
     * size of the compiled file, it may degrade performance due to the increased jmps introduced by
     * the loop.
     * 
     * Optimization #3:
     * This optimization groups together sequences of Incs and Decs, and IncPtrs and DecPtrs
     * Examples: 
     *      ++--+ is grouped as a single Inc(1) and -+-- is grouped as a single Dec(2)
     *      ><<><< is grouped as a single DecPtr(2) and >><>> is grouped as a single IncPtr(3)
     */
    internal class Compiler
    {
        public DILInstruction[] Instructions { get; private set; }
        public CompilationOptions Options { get; private set; }

        private LocalBuilder ptr;
        private LocalBuilder array;
        private Stack <Label> loopStack;
        private DILInstruction previousInstruction;

        /// <summary>
        /// How many times must an Input or Output operation be repeated before it's put into a for-loop
        /// 
        /// This constant is used for Optimization #2.
        /// </summary>
        private const int ThresholdForLoopIntroduction = 1; 

        public Compiler(IEnumerable<DILInstruction> instructions, CompilationOptions options = 0)
        {
            Instructions = instructions.ToArray();
            Options = options;
        }

        public void Compile(string filename)
        {
            var assembly = CreateAssemblyAndEntryPoint(filename);
            ILGenerator ilg = assembly.MainMethod.GetILGenerator();

            ptr = ilg.DeclareIntegerVariable();
            array = ilg.CreateArray<char>(0x493e0);

            loopStack = new Stack<Label>();

            var forLoopSpaceOptimizationStack = new Stack<ILForLoop>();

            for (int i = 0; i < Instructions.Length; i++)
            {
                var instruction = Instructions[i];
                if (i > 0)
                {
                    previousInstruction = Instructions[i - 1];
                }

                // If we're in debug mode, just emit the instruction as is and continue
                if (OptionEnabled(CompilationOptions.DebugMode)) 
                {
                    EmitInstruction(ilg, instruction);
                    continue;
                }

                /* Start of Optimization #3 */
                if ((instruction == DILInstruction.Inc || instruction == DILInstruction.Dec))
                {
                    var changes = CompactOppositeOperations(i, ilg, Increment, Decrement);
                    i += changes;
                    continue;
                }

                if (instruction == DILInstruction.IncPtr || instruction == DILInstruction.DecPtr)
                {
                    var changes = CompactOppositeOperations(i, ilg, IncrementPtr, DecrementPtr);
                    i += changes;
                    continue;
                }
                /* End of Optimization #3 */

                var nextEndLoopInstructionIndex = GetNextInstructionIndex(i, DILInstruction.EndLoop);

                /*
                 * If the current instruction is a StartLoop, make sure that there is a matching EndLoop, otherwise fail compilation
                 */
                if (instruction == DILInstruction.StartLoop && !nextEndLoopInstructionIndex.HasValue)
                {
                    throw new InstructionNotFoundException(String.Format("Expected to find an {0} instruction but didn't.", DILInstruction.StartLoop.ToString()));
                }

                /* Start of Optimization #1 
                    If either a) the current instruction is a StartLoop and it's preceeded by an EndLoop or
                              b) the current instruction is the first instruction and it's a StartLoop
                 *  completely ignore the loops and carry on.
                 */
                if (
                    (instruction == DILInstruction.StartLoop && previousInstruction == DILInstruction.EndLoop) //asdasd
                    || (instruction == DILInstruction.StartLoop && i == 0) //asd22
                    )
                {
                    i = nextEndLoopInstructionIndex.Value;
                    continue;
                }
                /* End of Optimization #1 */

                // If it's a loop instruction, emit the it without any optimizations
                if (instruction == DILInstruction.StartLoop || instruction == DILInstruction.EndLoop)
                {
                    EmitInstruction(ilg, instruction);
                    continue;
                }

                /* Optimization #2
                 *      The only instructions that arrive to this point are Input and Output 
                 */

                var repetitionTotal = GetTokenRepetitionTotal(i);
                if (repetitionTotal > ThresholdForLoopIntroduction) // Only introduce a loop if the repetition amount exceeds the threshold
                {
                    ILForLoop now;
                    if (forLoopSpaceOptimizationStack.Count > 0)
                    {
                        var last = forLoopSpaceOptimizationStack.Pop();
                        now = ilg.StartForLoop(last.Counter, last.Max, 0, repetitionTotal);
                    }
                    else
                    {
                        now = ilg.StartForLoop(0, repetitionTotal);
                    }

                    forLoopSpaceOptimizationStack.Push(now);

                    EmitInstruction(ilg, instruction);
                    ilg.EndForLoop(now);

                    i += repetitionTotal - 1;
                } 
                else
                {
                    EmitInstruction(ilg, instruction);
                }
            }

            ilg.Emit(OpCodes.Ret);

            Type t = assembly.MainClass.CreateType();
            assembly.DynamicAssembly.SetEntryPoint(assembly.MainMethod, PEFileKinds.ConsoleApplication);

            assembly.DynamicAssembly.Save(String.Format("{0}.exe", filename));
        }

        /// <summary>
        /// Used for Optimization #3.
        /// </summary>
        /// <returns></returns>
        private int CompactOppositeOperations(int index, ILGenerator ilg, Action<ILGenerator, int> positiveOperation, Action<ILGenerator, int> negativeOperation)
        {
            var instruction = Instructions[index];
            var changes = GetMatchingOperationChanges(index, ~instruction);
            if (instruction < 0)
            {
                changes.ChangesResult = -changes.ChangesResult;
            }

            if (changes.ChangesResult != 0)
            {
                if (changes.ChangesResult > 0)
                {
                    positiveOperation(ilg, changes.ChangesResult);
                }
                else
                {
                    negativeOperation(ilg, -changes.ChangesResult);
                }
            }

            return changes.TotalNumberOfChanges - 1;
        }

        private int? GetNextInstructionIndex(int index, DILInstruction dILInstruction)
        {
            for (int i = index; i < Instructions.Length; i++)
            {
                if (Instructions[i] == dILInstruction)
                {
                    return i;
                }
            }

            return null;
        }

        private MatchingOperationChanges GetMatchingOperationChanges(int index, DILInstruction matchingInstruction)
        {
            int total = 0, totalNumberOfChanges = 0;
            var currentInstruction = Instructions[index];
            for (int i = index; i < Instructions.Length; i++)
            {
                if (Instructions[i] == matchingInstruction || Instructions[i] == currentInstruction)
                {
                    total += Instructions[i] == Instructions[index] ? 1 : -1;
                    totalNumberOfChanges++;
                }
                else
                {
                    i = Instructions.Length;
                }
            }

            return new MatchingOperationChanges(total, totalNumberOfChanges);
        }

        private void EmitInstruction(ILGenerator ilg, DILInstruction instruction, int value = 1)
        {
            switch (instruction)
            {
                case DILInstruction.IncPtr: IncrementPtr(ilg, value); break;
                case DILInstruction.DecPtr: DecrementPtr(ilg, value); break;
                case DILInstruction.Inc: Increment(ilg, value); break;
                case DILInstruction.Dec: Decrement(ilg, value); break;
                case DILInstruction.Output:
                    {
                        ilg.Emit(OpCodes.Ldloc, array);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelem_U2);
                        ilg.EmitCall(OpCodes.Call,
                                     typeof(Console).GetMethods().First(
                                         m =>
                                         m.Name == "Write" && m.GetParameters().Length == 1 &&
                                         m.GetParameters().Any(p => p.ParameterType == typeof(char))),
                                     new[] { typeof(string) });
                        // TODO: Seriously find a better way how to invoke this one
                    }
                    break;
                case DILInstruction.Input:
                    {
                        ilg.Emit(OpCodes.Ldloc, array);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethod("Read"), null);
                        ilg.Emit(OpCodes.Conv_U2);
                        ilg.Emit(OpCodes.Stelem_I2);
                    }
                    break;
                case DILInstruction.StartLoop:
                    {
                        var L_0008 = ilg.DefineLabel();
                        ilg.Emit(OpCodes.Br, L_0008);
                        loopStack.Push(L_0008);

                        var L_0004 = ilg.DefineLabel();
                        ilg.MarkLabel(L_0004);
                        loopStack.Push(L_0004);
                    }
                    break;
                case DILInstruction.EndLoop:
                    {
                        Label go = loopStack.Pop(), mark = loopStack.Pop();
                        ilg.MarkLabel(mark);
                        ilg.Emit(OpCodes.Ldloc, array);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelem_U2);
                        ilg.Emit(OpCodes.Brtrue, go);
                    }
                    break;
            }
        }

        private void Increment(ILGenerator ilg, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelema, typeof(char));
            ilg.Emit(OpCodes.Dup);
            ilg.Emit(OpCodes.Ldobj, typeof(char));
            ilg.Emit(OpCodes.Ldc_I4, step);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stobj, typeof(char));
        }

        private void Decrement(ILGenerator ilg, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelema, typeof(char));
            ilg.Emit(OpCodes.Dup);
            ilg.Emit(OpCodes.Ldobj, typeof(char));
            ilg.Emit(OpCodes.Ldc_I4, step);
            ilg.Emit(OpCodes.Sub);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stobj, typeof(char));
        }

        private void IncrementPtr(ILGenerator ilg, int step = 1)
        {
            ilg.Increment(ptr, step);
        }

        private void DecrementPtr(ILGenerator ilg, int step = 1)
        {
            ilg.Decrement(ptr, step);
        }

        private AssemblyInfo CreateAssemblyAndEntryPoint(string filename)
        {
            var fileInfo = new FileInfo(filename);
            AssemblyName an = new AssemblyName { Name = fileInfo.Name };
            AppDomain ad = AppDomain.CurrentDomain;
            AssemblyBuilder ab = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);

            ModuleBuilder mb = ab.DefineDynamicModule(an.Name, String.Format("{0}.exe", filename), true);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder fb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);

            return new AssemblyInfo(ab, tb, fb);
        }

        private bool OptionEnabled(CompilationOptions option)
        {
            return (Options & option) == option;
        }

        private int GetTokenRepetitionTotal(int index)
        {
            var token = Instructions[index];
            int total = 0;
            for (int i = index; i < Instructions.Length; i++)
            {
                if (Instructions[i] == token)
                {
                    total++;
                }
                else
                {
                    break;
                }
            }

            return total;
        }
    }
}
