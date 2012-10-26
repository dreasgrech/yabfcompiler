
namespace YABFcompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    class Compiler
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
            var fileInfo = new FileInfo(filename);
            AssemblyName an = new AssemblyName {Name = fileInfo.Name};
            AppDomain ad = AppDomain.CurrentDomain;
            AssemblyBuilder ab = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);

            ModuleBuilder mb = ab.DefineDynamicModule(an.Name, String.Format("{0}.exe", filename), true);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder fb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);
            ILGenerator ilg = fb.GetILGenerator();

            ptr = ilg.DeclareInteger();
            array = ilg.CreateArray<char>(0x493e0);

            loopStack = new Stack<Label>();

            for (int i = 0; i < Instructions.Length; i++)
            {
                var instruction = Instructions[i];

                if (OptionEnabled(CompilationOptions.OptimizeForSpace))
                {
                    var repetitionTotal = GetTokenRepetitionTotal(i);
                    if (repetitionTotal > 1)
                    {
                        var loop = ilg.StartForLoop(0, repetitionTotal);
                        EmitInstruction(ilg, instruction);
                        ilg.EndForLoop(loop);

                        i += repetitionTotal - 1;
                        continue;
                    }
                }

                EmitInstruction(ilg, instruction);
            }

            ilg.Emit(OpCodes.Ret);

            Type t = tb.CreateType();
            ab.SetEntryPoint(fb, PEFileKinds.ConsoleApplication);

            ab.Save(String.Format("{0}.exe", filename));
        }

        private void EmitInstruction(ILGenerator ilg, DILInstruction instruction)
        {
            switch (instruction)
            {
                case DILInstruction.IncPtr:
                    {
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldc_I4_1);
                        ilg.Emit(OpCodes.Add);
                        ilg.Emit(OpCodes.Stloc, ptr);
                    }
                    break;
                case DILInstruction.DecPtr:
                    {
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldc_I4_1);
                        ilg.Emit(OpCodes.Sub);
                        ilg.Emit(OpCodes.Stloc, ptr);
                    }
                    break;
                case DILInstruction.Inc:
                    {
                        ilg.Emit(OpCodes.Ldloc, array);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelema, typeof(char));
                        ilg.Emit(OpCodes.Dup);
                        ilg.Emit(OpCodes.Ldobj, typeof(char));
                        ilg.Emit(OpCodes.Ldc_I4_1);
                        ilg.Emit(OpCodes.Add);
                        ilg.Emit(OpCodes.Conv_U2);
                        ilg.Emit(OpCodes.Stobj, typeof(char));
                    }
                    break;
                case DILInstruction.Dec:
                    {
                        ilg.Emit(OpCodes.Ldloc, array);
                        ilg.Emit(OpCodes.Ldloc, ptr);
                        ilg.Emit(OpCodes.Ldelema, typeof(char));
                        ilg.Emit(OpCodes.Dup);
                        ilg.Emit(OpCodes.Ldobj, typeof(char));
                        ilg.Emit(OpCodes.Ldc_I4_1);
                        ilg.Emit(OpCodes.Sub);
                        ilg.Emit(OpCodes.Conv_U2);
                        ilg.Emit(OpCodes.Stobj, typeof(char));
                    }
                    break;
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
