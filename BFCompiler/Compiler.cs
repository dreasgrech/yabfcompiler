
namespace YABFcompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using ILConstructs;

    internal class Compiler
    {
        public DILInstruction[] Instructions { get; private set; }
        public CompilationOptions Options { get; private set; }

        private LocalBuilder ptr;
        private LocalBuilder array;
        private Stack <Label> loopStack;

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

                // If it's either a loop instruction or debug mode is set, emit the instruction without any optimizations
                if (((instruction == DILInstruction.StartLoop || instruction == DILInstruction.EndLoop)) ||
                    OptionEnabled(CompilationOptions.DebugMode))
                {
                    EmitInstruction(ilg, instruction);
                    continue;
                }

                var repetitionTotal = GetTokenRepetitionTotal(i);

                if (repetitionTotal > 1)
                {
                    if (instruction == DILInstruction.Inc || instruction == DILInstruction.Dec)
                    {
                        if (instruction == DILInstruction.Inc)
                        {
                            Increment(ilg, repetitionTotal);
                        }
                        else
                        {
                            Decrement(ilg, repetitionTotal);
                        }
                    }
                    else if (instruction == DILInstruction.IncPtr || instruction == DILInstruction.DecPtr)
                    {
                        if (instruction == DILInstruction.IncPtr)
                        {
                            ilg.Increment(ptr, repetitionTotal);
                        }
                        else
                        {
                            ilg.Decrement(ptr, repetitionTotal);
                        }
                    }
                    else
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
                    }

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

        private void EmitInstruction(ILGenerator ilg, DILInstruction instruction, int value = 1)
        {
            switch (instruction)
            {
                case DILInstruction.IncPtr: ilg.Increment(ptr, value); break;
                case DILInstruction.DecPtr: ilg.Decrement(ptr, value); break;
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
