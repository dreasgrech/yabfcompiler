
namespace YABFcompiler.DIL.Operations
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Used to write a string literal
    /// </summary>
    class WriteLiteralOp : DILInstruction
    {
        public string Value { get; set; }

        private static readonly MethodInfo consoleWriteMethodInfo = typeof(Console).GetMethod("Write", new[] { typeof(string) });

        public WriteLiteralOp(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Emits a Console.Write(string) with its Value
        /// </summary>
        public void Emit(ILGenerator ilg, LocalBuilder array, LocalBuilder ptr)
        {
            ilg.Emit(OpCodes.Ldstr, Value);
            ilg.EmitCall(OpCodes.Call, consoleWriteMethodInfo, null);
        }
    }
}
