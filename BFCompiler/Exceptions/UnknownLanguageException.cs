
namespace YABFcompiler.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class UnknownLanguageException : Exception
    {
        public UnknownLanguageException()
        {
        }

        public UnknownLanguageException(string message) : base(message)
        {
        }

        public UnknownLanguageException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UnknownLanguageException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
