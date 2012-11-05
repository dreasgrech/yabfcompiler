
namespace CompilerTests
{
    using System;
    using System.Text;

    public class RunResults
    {
        public int ExitCode { get; set; }
        public Exception RunException { get; set; }
        public StringBuilder Output { get; set; }
        public StringBuilder Error { get; set; }
    }
}
