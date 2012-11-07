
using System.Collections.Generic;

namespace YABFcompiler
{
    using System.Reflection.Emit;
    using ILConstructs;

    internal static class ILGeneratorHelpers
    {
        public static Dictionary<int, OpCode> IntegerConstants32bit = new Dictionary<int, OpCode>
                                                                          {
                                                                              {0, OpCodes.Ldc_I4_0},
                                                                              {1, OpCodes.Ldc_I4_1},
                                                                              {2, OpCodes.Ldc_I4_2},
                                                                              {3, OpCodes.Ldc_I4_3},
                                                                              {4, OpCodes.Ldc_I4_4},
                                                                              {5, OpCodes.Ldc_I4_5},
                                                                              {6, OpCodes.Ldc_I4_6},
                                                                              {7, OpCodes.Ldc_I4_7},
                                                                              {8, OpCodes.Ldc_I4_8},
                                                                              //{9, OpCodes.Ldc_I4_S},
                                                                          };
        public static LocalBuilder Increment(this ILGenerator ilg, LocalBuilder variable, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, variable);
            Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, variable);
            return variable;
        }

        public static LocalBuilder Decrement(this ILGenerator ilg, LocalBuilder variable, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, variable);
            Load32BitIntegerConstant(ilg, step);
            ilg.Emit(OpCodes.Sub);
            ilg.Emit(OpCodes.Stloc, variable);
            return variable;
        }

        public static LocalBuilder DeclareIntegerVariable(this ILGenerator ilg, int value = 0)
        {
            LocalBuilder intVariable = ilg.DeclareLocal(typeof(int));
            Load32BitIntegerConstant(ilg, value);
            ilg.Emit(OpCodes.Stloc, intVariable);

            return intVariable;
        }

        public static LocalBuilder ReassignIntegerVariable(this ILGenerator ilg, LocalBuilder variable, int value = 0)
        {
            Load32BitIntegerConstant(ilg, value);
            ilg.Emit(OpCodes.Stloc, variable);

            return variable;
        }

        public static LocalBuilder CreateArray<T>(this ILGenerator ilg, int size, string name = "")
        {
            LocalBuilder array = ilg.DeclareLocal(typeof(T[]));
            array.SetLocalSymInfo(name);
            Load32BitIntegerConstant(ilg, size);
            ilg.Emit(OpCodes.Newarr, typeof(T));
            ilg.Emit(OpCodes.Stloc, array);
            return array;
        }

        public static ILForLoop StartForLoop(this ILGenerator ilg, LocalBuilder startVariable, LocalBuilder maximumVariable, int start, int maximum)
        {
            startVariable = ilg.ReassignIntegerVariable(startVariable, start);
            maximumVariable = ilg.ReassignIntegerVariable(maximumVariable, maximum);

            return ilg.StartForLoop(startVariable, maximumVariable);
        }

        public static ILForLoop StartForLoop(this ILGenerator ilg, int start, int maximum)
        {
            LocalBuilder max = ilg.DeclareIntegerVariable(maximum),
                         counter = ilg.DeclareIntegerVariable(start);

            return ilg.StartForLoop(counter, max);
        }

        public static void EndForLoop(this ILGenerator ilg, ILForLoop forLoop)
        {
            ilg.Emit(OpCodes.Ldloc, forLoop.Counter);
            Load32BitIntegerConstant(ilg, 1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, forLoop.Counter);

            ilg.MarkLabel(forLoop.ConditionLabel);
            ilg.Emit(OpCodes.Ldloc, forLoop.Counter);
            ilg.Emit(OpCodes.Ldloc, forLoop.Max);
            ilg.Emit(OpCodes.Blt, forLoop.StartLoopLogicLabel);
        }

        public static void Load32BitIntegerConstant(ILGenerator ilg, int constant)
        {
            if (IntegerConstants32bit.ContainsKey(constant))
            {
                ilg.Emit(IntegerConstants32bit[constant]);
                return;
            }

            ilg.Emit(OpCodes.Ldc_I4, constant);
        }

        private static ILForLoop StartForLoop(this ILGenerator ilg, LocalBuilder counterVariable, LocalBuilder maximumVariable)
        {
            var conditionLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br, conditionLabel);

            var startLoopLogicLabel = ilg.DefineLabel();
            ilg.MarkLabel(startLoopLogicLabel);

            return new ILForLoop(conditionLabel, startLoopLogicLabel, counterVariable, maximumVariable);
        }
    }
}
