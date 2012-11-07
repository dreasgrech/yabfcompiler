
namespace YABFcompiler.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class CompilerException : Exception
    {
        public CompilerException() {}
        public CompilerException(string message) : base(message) { }
        public CompilerException(string message, Exception inner) : base(message, inner) {}
        public CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) {}

    }
}
