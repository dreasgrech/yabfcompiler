using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace YABFcompiler
{
    class ILForLoop
    {
        public Label ConditionLabel { get; private set; }
        public Label StartLoopLogicLabel { get; private set; }
        public LocalBuilder Counter { get; private set; }
        public LocalBuilder Max { get; private set; }

        public ILForLoop(Label conditionLabel, Label startLoopLogicLabel, LocalBuilder counter, LocalBuilder max)
        {
            ConditionLabel = conditionLabel;
            Counter = counter;
            Max = max;
            StartLoopLogicLabel = startLoopLogicLabel;
        }
    }

    internal static class ILGeneratorHelpers
    {
        public static LocalBuilder Increment(this ILGenerator ilg, LocalBuilder variable, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, variable);
            ilg.Emit(OpCodes.Ldc_I4, step);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, variable);
            return variable;
        }

        public static LocalBuilder Decrement(this ILGenerator ilg, LocalBuilder variable, int step = 1)
        {
            ilg.Emit(OpCodes.Ldloc, variable);
            ilg.Emit(OpCodes.Ldc_I4, step);
            ilg.Emit(OpCodes.Sub);
            ilg.Emit(OpCodes.Stloc, variable);
            return variable;
        }

        public static LocalBuilder DeclareIntegerVariable(this ILGenerator ilg, int value = 0)
        {
            LocalBuilder intVariable = ilg.DeclareLocal(typeof(int));
            ilg.Emit(OpCodes.Ldc_I4, value);
            ilg.Emit(OpCodes.Stloc, intVariable);

            return intVariable;
        }

        public static LocalBuilder ReassignIntegerVariable(this ILGenerator ilg, LocalBuilder variable, int value = 0)
        {
            ilg.Emit(OpCodes.Ldc_I4, value);
            ilg.Emit(OpCodes.Stloc, variable);

            return variable;
        }

        public static LocalBuilder CreateArray<T>(this ILGenerator ilg, int size, string name = "")
        {
            LocalBuilder array = ilg.DeclareLocal(typeof(T[]));
            array.SetLocalSymInfo(name);
            ilg.Emit(OpCodes.Ldc_I4, size);
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
            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Stloc, forLoop.Counter);

            ilg.MarkLabel(forLoop.ConditionLabel);
            ilg.Emit(OpCodes.Ldloc, forLoop.Counter);
            ilg.Emit(OpCodes.Ldloc, forLoop.Max);
            ilg.Emit(OpCodes.Blt, forLoop.StartLoopLogicLabel);
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
