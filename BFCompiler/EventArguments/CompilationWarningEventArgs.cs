
namespace YABFcompiler.EventArguments
{
    using System;

    class CompilationWarningEventArgs : EventArgs
    {
        public string Message { get; set; }

        public CompilationWarningEventArgs(string format, params object[] args)
        {
            Message = String.Format(format, args);
        }
    }
}
