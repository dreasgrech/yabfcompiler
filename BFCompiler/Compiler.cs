﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace BFCompiler
{
    class Compiler
    {
        public IEnumerable<DILInstruction> Instructions { get; private set; }

        private List<char> domain; 

        public Compiler(IEnumerable<DILInstruction> instructions)
        {
            Instructions = instructions;
            domain = new List<char>();
        }

        public void Compile(string filename)
        {
            var fileInfo = new FileInfo(filename);
            AssemblyName an = new AssemblyName {Name = fileInfo.Name};
            AppDomain ad = AppDomain.CurrentDomain;
            AssemblyBuilder ab = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);

            ModuleBuilder mb = ab.DefineDynamicModule(an.Name, String.Format("{0}.exe", filename), true);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder fb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, new [] { typeof(string[]) });
            ILGenerator ilg = fb.GetILGenerator();

           // var n = CreatePrimitive(ilg, typeof (int));

            LocalBuilder ptr = ilg.DeclareLocal(typeof(int));
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Stloc, ptr);

            var array = CreateArray<char>(ilg, 0x493e0, "array");

            foreach (var instruction in Instructions)
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
                            ilg.EmitCall(OpCodes.Call, typeof(Console).GetMethods().First(m => m.Name == "Write" && m.GetParameters().Length == 1 && m.GetParameters().Any(p => p.ParameterType == typeof(char))), new[] { typeof(string) });
                        }
                        break;
                }
            }

            //ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);

            Type t = tb.CreateType();
            // Set the entrypoint (thereby declaring it an EXE)
            ab.SetEntryPoint(fb, PEFileKinds.ConsoleApplication);

            // Save it
            ab.Save(String.Format("{0}.exe", filename));

        }

        private LocalBuilder CreatePrimitive(ILGenerator ilg, Type localType, string name="")
        {
            //LocalBuilder loc = ilg.DeclareLocal(localType);
            //loc.SetLocalSymInfo(name);

            return null;//loc;
        }

        private LocalBuilder CreateArray<T>(ILGenerator ilg, int size, string name = "")
        {
            LocalBuilder array = ilg.DeclareLocal(typeof(T[]));
            array.SetLocalSymInfo(name);
            ilg.Emit(OpCodes.Ldc_I4, size); // 30000
            ilg.Emit(OpCodes.Newarr, typeof(T));
            ilg.Emit(OpCodes.Stloc, array);
            return array;
        }

        private void IncrementDomainCell(int ptr)
        {
            
        }
    }
}
