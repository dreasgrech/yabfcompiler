
using YABFcompiler.DIL;

namespace YABFcompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using EventArguments;
    using Exceptions;
    using ILConstructs;

    /*
     * ASSUMPTIONS
     * Assumption #1:
     *  Every cell is initialized with 0
     *  
     * OPTIMIZATIONS
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
     *      +++--- is eliminated since it's basically a noop
     *      ><<><< is grouped as a single DecPtr(2) and >><>> is grouped as a single IncPtr(3)
     *      >>><<< is eliminated since it's basically a noop
     *      
     * Optimization #4:
     * Some patterns of clearance loops are detected and replaced with Assign(0)
     * Examples:
     *      [-], [+]
     *      
     * Optimization #5:
     * Simple loop code walking.
     * 
     * A simple loop doesn't contain any input or out, and it also doesn't contain nested loop.
     * A simple loop also returns to the starting position after execution.  Meaning that the position of StartLoop is equal to the position of EndLoop.
     * 
     * These simple loops are replaced with multiplication operations.
     * 
     * So for ++[>+++<-] the emitted code will be:
     * 
     *     chArray[index] = (char) (chArray[index] + '\x0002');
     *     chArray[index + 1] = (char) (chArray[index + 1] + ((char) (chArray[index] * '\x0003')));
     *     chArray[index] = '\0';
     * 
     * Notice how the loop in brainfuck was replaced by a multiplication operation and an assigment, 
     * the assignment being the NULL to the starting position of the loop since that's the 
     * reason why the loop was halted.
     */

    public class Compiler
    {

        /// <summary>
        /// How many times must an Input or Output operation be repeated before it's put into a for-loop
        /// 
        /// This constant is used for Optimization #2.
        /// </summary>
        private const int ThresholdForLoopIntroduction = 4;

        /// <summary>
        /// The size of the array domain for brainfuck to work in
        /// </summary>
        private const int DomainSize = 0x493e0;

        public Parser Parser { get; private set; }
        public CompilationOptions Options { get; private set; }

        private LanguageInstruction[] instructions;
        private readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(char) });
        private readonly MethodInfo consoleReadMethodInfo = typeof(Console).GetMethod("Read");
        private DILOperationSet dilInstructions; 
        public event EventHandler<CompilationWarningEventArgs> OnWarning;

        private LocalBuilder ptr;
        private LocalBuilder array;
        private Stack<Label> loopStack;
        private LanguageInstruction previousInstruction;
        private readonly Stack<LanguageInstruction> whileLoopStack = new Stack<LanguageInstruction>();

        public Compiler(Parser parser, CompilationOptions options = 0)
        {
            Parser = parser;
            Options = options;
        }

        /// <summary>
        /// Returns the location of the compiled assembly
        /// </summary>
        /// <returns></returns>
        public string Compile(string filename)
        {
            instructions = Parser.GenerateDIL(File.ReadAllText(filename)).ToArray();
            dilInstructions = new DILOperationSet();

            var assembly = CreateAssemblyAndEntryPoint(filename);
            ILGenerator ilg = assembly.MainMethod.GetILGenerator();

            ptr = ilg.DeclareIntegerVariable();
            array = ilg.CreateArray<char>(DomainSize);

            loopStack = new Stack<Label>();

            var dilLoopStack = new Stack<LoopOp>();
            var forLoopSpaceOptimizationStack = new Stack<ILForLoop>();

            LanguageInstruction? areLoopOperationsBalanced;
            if ((areLoopOperationsBalanced = AreLoopOperationsBalanced()) != null)
            {
                throw new InstructionNotFoundException(String.Format("Expected to find an {0} instruction but didn't.", (~areLoopOperationsBalanced.Value).ToString()));
            }

            for (int i = 0; i < instructions.Length; i++)
            {
                var instruction = instructions[i];
                if (i > 0)
                {
                    previousInstruction = instructions[i - 1];
                }

                // If we're in debug mode, just emit the instruction as is and continue
                if (OptionEnabled(CompilationOptions.DebugMode))
                {
                    EmitInstruction(ilg, instruction);
                    continue;
                }

                /* Start of Optimization #3 */
                if ((instruction == LanguageInstruction.Inc || instruction == LanguageInstruction.Dec))
                {
                    dilInstructions.Add(new AdditionMemoryOp(0, instruction == LanguageInstruction.Inc ? 1 : -1));

                    //if (AreWeInALoop())
                    //{
                    //    var changes = CompactOppositeOperations(i, ilg, Increment, Decrement);
                    //    i += changes;
                    //    continue;
                    //}

                    //i += ApplySimpleWalkResults(ilg, i) - 1;

                    continue;
                }

                if (instruction == LanguageInstruction.IncPtr || instruction == LanguageInstruction.DecPtr)
                {
                    dilInstructions.Add(new PtrOp(instruction == LanguageInstruction.IncPtr ? 1 : -1));
                    
                    //if (AreWeInALoop())
                    //{
                    //    var changes = CompactOppositeOperations(i, ilg, IncrementPtr, DecrementPtr);
                    //    i += changes;
                    //    continue;
                    //}

                    //i += ApplySimpleWalkResults(ilg, i) - 1;
                    continue;
                }
                /* End of Optimization #3 */


                /* Start of Optimization #1 
                    If either a) the current instruction is a StartLoop and it's preceeded by an EndLoop or
                              b) the current instruction is the first instruction and it's a StartLoop
                 *  completely skip the loops and carry on.
                 */
                if (
                    (instruction == LanguageInstruction.StartLoop && previousInstruction == LanguageInstruction.EndLoop)
                    || (instruction == LanguageInstruction.StartLoop && i == 0)
                    )
                {
                    var nextEndLoopInstructionIndex = GetNextClosingLoopIndex(i);
                    i = nextEndLoopInstructionIndex.Value; // nextEndLoopInstructionIndex will always have a value because we verified the number of StartLoop and EndLoop operations at the beginning
                    continue;
                }
                /* End of Optimization #1 */

                if (instruction == LanguageInstruction.StartLoop || instruction == LanguageInstruction.EndLoop)
                {
                    if (instruction == LanguageInstruction.StartLoop)
                    {
                        var loop = Loop.Construct(instructions, i);
                        //dilLoopStack.Push(new LoopOp(loop));
                        dilInstructions.Add(new LoopOp(loop));

                        i += loop.Instructions.Length + 1;
                        continue;

                        if (loop.IsSimple())
                        {
                            
                        }

                        /* Start of Optimization #4*/
                        if (loop.IsClearanceLoop())
                        {
                            AssignValue(ilg, 0);
                            i += loop.Instructions.Length + 1;
                            continue;
                        }
                        /* End of Optimization #4*/

                        if (loop.IsInfiniteLoopPattern())
                        {
                            if (OnWarning != null)
                            {
                                OnWarning(this,
                                          new CompilationWarningEventArgs(
                                              "Infinite loop pattern detected at cell {0}: [{1}]", i,
                                              String.Concat(loop.Instructions)));
                            }
                        }

                        //var isSimple = loop.IsSimple();

                        /* Start of Optimization #5 */
                        // TODO: Currently working on Optimization #5 for nested loops
                        if (IsSimpleLoop(i) != null) // TODO: needs to be changed to loop.IsSimple()
                        {
                            //var walkResults = CalculateSimpleWalkResults(i + 1);

                            //walkResults.IterateDomain((cellIndex, cellDelta) =>
                            //                              {
                            //                                  if (cellIndex == 0)
                            //                                  {
                            //                                      AssignValue(ilg, 0);
                            //                                      return;
                            //                                  }

                            //                                  MultiplyByIndexValue(ilg, cellIndex, cellDelta);
                            //                              });

                            //i += walkResults.TotalInstructionsCovered + 1;
                            //continue;
                        }
                        /* End of Optimization #5 */
                    }

                    if (instruction == LanguageInstruction.EndLoop)
                    {
                        whileLoopStack.Pop();
                    }
                    else
                    {
                        whileLoopStack.Push(LanguageInstruction.StartLoop);
                    }

                    //EmitInstruction(ilg, instruction);
                    continue;
                }

                /* The only instructions that arrive to this point are Input and Output  */

                var repetitionTotal = GetTokenRepetitionTotal(i);

                /* 
                 * Optimization #2
                 * 
                 * Only introduce a loop if the repetition amount exceeds the threshold
                 */
                if (repetitionTotal > ThresholdForLoopIntroduction)
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
                    switch (instruction)
                    {
                        case LanguageInstruction.Output: dilInstructions.Add(new WriteOp()); break;
                        case LanguageInstruction.Input: dilInstructions.Add(new ReadOp()); break;
                    }

                    //EmitInstruction(ilg, instruction);
                }
            }

            while (dilInstructions.Optimize(ref dilInstructions))
            {
            }


            EmitDIL(ilg);



            ilg.Emit(OpCodes.Ret);

            Type t = assembly.MainClass.CreateType();
            assembly.DynamicAssembly.SetEntryPoint(assembly.MainMethod, PEFileKinds.ConsoleApplication);

            var compiledAssemblyFile = String.Format("{0}.exe", Path.GetFileNameWithoutExtension(filename));
            assembly.DynamicAssembly.Save(compiledAssemblyFile);

            return compiledAssemblyFile;
        }

        /// <summary>
        /// Returns true if the current operation is part of a loop
        /// </summary>
        /// <returns></returns>
        private bool AreWeInALoop()
        {
            return whileLoopStack.Count > 0;
        }

        /// <summary>
        /// Returns null if the number of StartLoop operations match the number of EndLoop operations
        /// 
        /// Otherwise, it returns the operation with the excess total.
        /// </summary>
        private LanguageInstruction? AreLoopOperationsBalanced()
        {
            int totalStartLoopOperations = instructions.Where(instruction => instruction == LanguageInstruction.StartLoop).Count(),
                totalEndLoopOperations = instructions.Where(instruction => instruction == LanguageInstruction.EndLoop).Count();

            if (totalStartLoopOperations == totalEndLoopOperations)
            {
                return null;
            }

            if (totalStartLoopOperations > totalEndLoopOperations)
            {
                return LanguageInstruction.StartLoop;
            }

            return LanguageInstruction.EndLoop;
        }

        /// <summary>
        /// TODO: This method needs to be replaced with the one in Loop.IsSimple()
        /// 
        /// Returns the operations contained within the loop is the loop is a simple loop
        /// 
        /// A simple loop doesn't contain any input or out, and it also doesn't contain nested loop
        /// A simple loop also returns to the starting position after execution.  Meaning that the position of StartLoop is equal to the position of EndLoop.
        /// </summary>
        /// <returns></returns>
        private LanguageInstruction[] IsSimpleLoop(int index)
        {
            var loopInstructions = GetLoopInstructions(index);
            bool containsIO = loopInstructions.Any(i =>
                i == LanguageInstruction.Input || i == LanguageInstruction.Output
                || i == LanguageInstruction.StartLoop || i == LanguageInstruction.EndLoop // I'm excluding nested loops for now
                );

            if (containsIO /* and nested loops */)
            {
                return null;
            }

            int totalIncPtrs = loopInstructions.Count(i => i == LanguageInstruction.IncPtr),
                totalDecPtrs = loopInstructions.Count(i => i == LanguageInstruction.DecPtr);

            var returnsToStartLoopPosition = totalDecPtrs == totalIncPtrs;

            if (!returnsToStartLoopPosition)
            {
                return null;
            }

            return loopInstructions;
        }

        public int AddOperationToDomain(SortedDictionary<int, int> domain, int index, int step = 1)
        {
            if (domain.ContainsKey(index))
            {
                domain[index] += step;
                return domain[index];
            }

            domain.Add(index, step);
            return step;
        }

        /// <summary>
        /// TODO: Need to make it work for when the ptr goes below 0
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="index"></param>
        /// <param name="stopWalking"></param>
        /// <returns></returns>
        private WalkResults SimpleWalk(IEnumerable<LanguageInstruction> operations, int index, int stopWalking)
        {
            int ptrIndex = 0;
            var domain = new SortedDictionary<int, int>();

            var ins = operations.Skip(index).Take(stopWalking - index).ToArray();

            foreach (var instruction in ins)
            {
                switch (instruction)
                {
                    case LanguageInstruction.IncPtr: ptrIndex++; break;
                    case LanguageInstruction.DecPtr: ptrIndex--; break;
                    case LanguageInstruction.Inc: AddOperationToDomain(domain, ptrIndex); break;
                    case LanguageInstruction.Dec: AddOperationToDomain(domain, ptrIndex, -1); break;
                }
            }

            return new WalkResults(domain, ptrIndex, ins.Count());
        }

        private WalkResults CalculateSimpleWalkResults(int index)
        {
            var end = instructions.Length;
            int whereToStop = Math.Min(Math.Min(
                Math.Min(GetNextInstructionIndex(index, LanguageInstruction.StartLoop) ?? end, GetNextInstructionIndex(index, LanguageInstruction.Input) ?? end),
                GetNextInstructionIndex(index, LanguageInstruction.Output) ?? end), GetNextInstructionIndex(index, LanguageInstruction.EndLoop) ?? end);

            return SimpleWalk(instructions, index, whereToStop);
        }

        private int ApplySimpleWalkResults(ILGenerator ilg, int index)
        {
            var walkResults = CalculateSimpleWalkResults(index);

            int ptrPosition = 0;
            foreach (var cell in walkResults.Domain)
            {
                var needToGo = cell.Key - ptrPosition;
                ptrPosition += needToGo;
                if (needToGo > 0)
                {
                    //IncrementPtr(ilg, needToGo);
                    dilInstructions.Add(new PtrOp(needToGo));
                }
                else if (cell.Key < 0)
                {
                    //DecrementPtr(ilg, -needToGo);
                    dilInstructions.Add(new PtrOp(needToGo));
                }

                if (cell.Value > 0)
                {
                    //Increment(ilg, cell.Value);
                    dilInstructions.Add(new AdditionMemoryOp(0, cell.Value));
                }
                else if (cell.Value < 0)
                {
                    //Decrement(ilg, -cell.Value);
                    dilInstructions.Add(new AdditionMemoryOp(0, cell.Value));
                }
            }

            /*
             * If there were no cell changes but the pointer still moved, we need to assign the new position
             * of the pointer.
             */
            if (ptrPosition != walkResults.EndPtrPosition)
            {
                var delta = walkResults.EndPtrPosition - ptrPosition;
                if (delta > 0)
                {
                    //IncrementPtr(ilg, delta);
                    dilInstructions.Add(new PtrOp(delta));
                }
                else if (delta < 0)
                {
                    //DecrementPtr(ilg, -delta);
                    dilInstructions.Add(new PtrOp(delta));
                }
            }

            return walkResults.TotalInstructionsCovered;
        }

        /// <summary>
        /// Returns the instructions the loop contains
        /// </summary>
        /// <param name="index">The index of the StartLoop instruction</param>
        /// <returns></returns>
        private LanguageInstruction[] GetLoopInstructions(int index)
        {
            var closingEndLoopIndex = GetNextClosingLoopIndex(index).Value;
            return instructions.Skip(index + 1).Take(closingEndLoopIndex - index - 1).ToArray();
        }

        /// <summary>
        /// Used for Optimization #3.
        /// </summary>
        /// <returns></returns>
        private int CompactOppositeOperations(int index, ILGenerator ilg, Action<ILGenerator, int> positiveOperation, Action<ILGenerator, int> negativeOperation)
        {
            var instruction = instructions[index];
            var changes = GetMatchingOperationChanges(index);
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

        /// <summary>
        /// Returns the index of the EndLoop for the given StartLoop
        /// 
        /// Returns null if a matching EndLoop is not found
        /// </summary>
        /// <param name="index">The index of the StartLoop instruction</param>
        /// <returns></returns>
        private int? GetNextClosingLoopIndex(int index)
        {
            int stack = 0;

            for (int i = index + 1; i < instructions.Length; i++)
            {
                if (instructions[i] == LanguageInstruction.StartLoop)
                {
                    stack += 1;
                }

                if (instructions[i] == LanguageInstruction.EndLoop)
                {
                    if (stack > 0)
                    {
                        stack--;
                        continue;
                    }

                    return i;
                }
            }

            return null;
        }

        private int? GetNextInstructionIndex(int index, LanguageInstruction instruction)
        {
            for (int i = index; i < instructions.Length; i++)
            {
                if (instructions[i] == instruction)
                {
                    return i;
                }
            }

            return null;
        }

        private MatchingOperationChanges GetMatchingOperationChanges(int index)
        {
            int total = 0, totalNumberOfChanges = 0;
            LanguageInstruction currentInstruction = instructions[index],
                           matchingInstruction = ~currentInstruction;

            for (int i = index; i < instructions.Length; i++)
            {
                if (instructions[i] == matchingInstruction || instructions[i] == currentInstruction)
                {
                    total += instructions[i] == instructions[index] ? 1 : -1;
                    totalNumberOfChanges++;
                }
                else
                {
                    i = instructions.Length;
                }
            }

            return new MatchingOperationChanges(total, totalNumberOfChanges);
        }

        private void EmitDIL(ILGenerator ilg)
        {
            foreach (var dilInstruction in dilInstructions)
            {
                if (dilInstruction is AdditionMemoryOp)
                {
                    var add = (AdditionMemoryOp) dilInstruction;
                    if (add.Scalar > 0)
                    {
                        Increment(ilg, add.Constant, add.Offset, add.Scalar);
                    } else
                    {
                        Decrement(ilg, add.Constant, add.Offset, - add.Scalar);
                    }
                } 
                else if (dilInstruction is PtrOp)
                {
                    var ptrOp = (PtrOp) dilInstruction;
                    if (ptrOp.Delta > 0)
                    {
                        IncrementPtr(ilg, ptrOp.Delta);
                    } else
                    {
                        DecrementPtr(ilg, -ptrOp.Delta);
                    }
                } 
                else if (dilInstruction is WriteOp)
                {
                    Output(ilg, ((WriteOp)dilInstruction).Constant);
                } 
                else if (dilInstruction is ReadOp)
                {
                    Input(ilg, ((ReadOp)dilInstruction).Constant);
                } else if (dilInstruction is AssignOp)
                {
                    var assign = dilInstruction as AssignOp;
                    AssignValue(ilg, assign.Constant, assign.Value);
                }
            }
        }
        #region Instruction emitters
        private void EmitInstruction(ILGenerator ilg, LanguageInstruction instruction, int value = 1)
        {
            switch (instruction)
            {
                case LanguageInstruction.IncPtr: IncrementPtr(ilg, value); break;
                case LanguageInstruction.DecPtr: DecrementPtr(ilg, value); break;
                case LanguageInstruction.Inc: Increment(ilg, value); break;
                case LanguageInstruction.Dec: Decrement(ilg, value); break;
                case LanguageInstruction.Output: Output(ilg); break;
                case LanguageInstruction.Input: Input(ilg); break;
                case LanguageInstruction.StartLoop:
                    {
                        var L_0008 = ilg.DefineLabel();
                        ilg.Emit(OpCodes.Br, L_0008);
                        loopStack.Push(L_0008);

                        var L_0004 = ilg.DefineLabel();
                        ilg.MarkLabel(L_0004);
                        loopStack.Push(L_0004);
                    }
                    break;
                case LanguageInstruction.EndLoop:
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

        private void Input(ILGenerator ilg, ConstantValue constant = null)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }

            ilg.EmitCall(OpCodes.Call, consoleReadMethodInfo, null);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        private void Output(ILGenerator ilg, ConstantValue constant = null)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            } 
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }
            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
        }

        /// <summary>
        /// Emit instructions to assign an integer constant to the value of the current cell
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="value"></param>
        private void AssignValue(ILGenerator ilg, int value = 1)
        {
            AssignValue(ilg, null, value);
        }

        private void AssignValue(ILGenerator ilg, ConstantValue constant, int value = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            } 
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
            }
            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, value);
            ilg.Emit(OpCodes.Stelem_I2);
        }


        private void Increment(ILGenerator ilg, ConstantValue constant, int offset, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            } else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    } else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldloc, array);
            if (constant != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constant.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }
            ilg.Emit(OpCodes.Ldelem_U2);
            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        /// <summary>
        /// Emit instructions to increment the value of the current cell by an integer constant
        /// </summary>
        private void Increment(ILGenerator ilg, int step = 1)
        {
            Increment(ilg, null, step);
        }

        private void Decrement(ILGenerator ilg, ConstantValue constantValue, int offset, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            if (constantValue != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constantValue.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldloc, array);
            if (constantValue != null)
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, constantValue.Value);
            }
            else
            {
                ilg.Emit(OpCodes.Ldloc, ptr);
                if (offset != 0)
                {
                    ILGeneratorHelpers.Load32BitIntegerConstant(ilg, Math.Abs(offset));
                    if (offset > 0)
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                }
            }

            ilg.Emit(OpCodes.Ldelem_U2);
            ILGeneratorHelpers.Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Sub);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        /// <summary>
        /// Emit instructions to decrement the value of the current cell by an integer constant
        /// </summary>
        private void Decrement(ILGenerator ilg, int step = 1)
        {
            Decrement(ilg, null, step);
        }

        /// <summary>
        /// Given an offset of 2 and scalar of 3, generates:
        /// chArray[index + 2] = (char) (chArray[index + 2] + ((char) (chArray[index] * '\x0003')));
        /// 
        /// If the scalar is 1, no multiplication is done:
        /// chArray[index + 2] = (char) (chArray[index + 2] + chArray[index]);
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="offset"></param>
        /// <param name="scalar"></param>
        private void MultiplyByIndexValue(ILGenerator ilg, int offset, int scalar)
        {
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);

            if (offset != 0)
            {
                OpCode instruction;
                int os = offset;
                if (offset > 0)
                {
                    instruction = OpCodes.Add;
                }
                else
                {
                    instruction = OpCodes.Sub;
                    os = -os;
                }

                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, os);
                ilg.Emit(instruction);
            }

            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            if (offset != 0)
            {
                OpCode instruction;
                int os = offset;
                if (offset > 0)
                {
                    instruction = OpCodes.Add;
                }
                else
                {
                    instruction = OpCodes.Sub;
                    os = -os;
                }

                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, os);
                ilg.Emit(instruction);
            }

            ilg.Emit(OpCodes.Ldelem_U2);
            ilg.Emit(OpCodes.Ldloc, array);
            ilg.Emit(OpCodes.Ldloc, ptr);
            ilg.Emit(OpCodes.Ldelem_U2);
            if (scalar != 1) // multiply only if the scalar is != 1
            {
                ILGeneratorHelpers.Load32BitIntegerConstant(ilg, scalar);
                ilg.Emit(OpCodes.Mul);
                ilg.Emit(OpCodes.Conv_U2);
            }

            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Conv_U2);
            ilg.Emit(OpCodes.Stelem_I2);
        }

        /// <summary>
        /// Emit instructions to increment the pointer position by an integer constant
        /// </summary>
        private void IncrementPtr(ILGenerator ilg, int step = 1)
        {
            ilg.Increment(ptr, step);
        }

        /// <summary>
        /// Emit instructions to decrement the pointer position by an integer constant
        /// </summary>
        private void DecrementPtr(ILGenerator ilg, int step = 1)
        {
            ilg.Decrement(ptr, step);
        }
        #endregion

        private AssemblyInfo CreateAssemblyAndEntryPoint(string filename)
        {
            var fileInfo = new FileInfo(filename);
            AssemblyName an = new AssemblyName { Name = fileInfo.Name };
            AppDomain ad = AppDomain.CurrentDomain;
            AssemblyBuilder ab = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);

            ModuleBuilder mb = ab.DefineDynamicModule(an.Name, String.Format("{0}.exe", Path.GetFileNameWithoutExtension(filename)), true);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder fb = tb.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, null, null);

            return new AssemblyInfo(ab, tb, fb);
        }

        private bool OptionEnabled(CompilationOptions option)
        {
            return (Options & option) == option;
        }

        /// <summary>
        /// Gets the number of repeating tokens starting from the provided index
        /// 
        /// So, +++-[] will return 3 because of the first three consecutive Incs but -+++-[] will return 0.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GetTokenRepetitionTotal(int index)
        {
            var token = instructions[index];
            int total = 0;
            for (int i = index; i < instructions.Length; i++)
            {
                if (instructions[i] == token)
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