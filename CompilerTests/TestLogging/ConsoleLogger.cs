
namespace CompilerTests.TestLogging
{
    using System;

    /// <summary>
    /// Log to the Console
    /// </summary>
    internal class ConsoleLogger : Logger
    {
        protected override void Write(ConsoleColor color, string messageFormat, params object[] messageArgs)
        {
            Console.ForegroundColor = color;
            Console.Write(messageFormat, messageArgs);
            Console.ResetColor();
        }
    }
}
