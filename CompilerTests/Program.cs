
namespace CompilerTests
{
    using System;
    using System.IO;
    using TestLogging;

    internal class Program
    {
        private static readonly string MainDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Programs");
        private const string customLanguageFileExtension = "yabfc";

        private static void Main(string[] args)
        {
            Console.SetWindowSize(200, 50);

            var logger = new ConsoleLogger();
            var testRunner = new TestRunner(MainDirectory, customLanguageFileExtension, logger);
            testRunner.RunTests();
        }
    }
}
