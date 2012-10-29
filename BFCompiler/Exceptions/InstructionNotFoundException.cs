
namespace YABFcompiler.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class InstructionNotFoundException : CompilerException
    {

        public InstructionNotFoundException()
        {
        }

        public InstructionNotFoundException(string message) : base(message)
        {
        }

        public InstructionNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InstructionNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
