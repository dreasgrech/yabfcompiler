
namespace CompilerTests.TestLogging
{
    using System;
    using System.IO;

    internal abstract class Logger
    {
        protected abstract void Write(ConsoleColor color, string messageFormat, params object[] messageArgs);

        public void LogLanguageName(string languageDirectory)
        {
            WriteTestRunnerMessage(ConsoleColor.Green, "Running tests for ");
            Write(ConsoleColor.Red, new DirectoryInfo(languageDirectory).Name + "\r\n");
        }

        public void WriteLineTestRunnerMessage(ConsoleColor color, string messageFormat, params object[] messageArgs)
        {
            WriteTestRunnerMessage(color, messageFormat + "\r\n", messageArgs);
        }

        public void WriteTestRunnerMessage(ConsoleColor color, string messageFormat, params object[] messageArgs)
        {
            Write(color, "TEST RUNNER => ");
            Write(Console.ForegroundColor, messageFormat, messageArgs);
        }
    }
}
