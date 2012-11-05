
namespace CompilerTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using TestLogging;
    using YABFcompiler;

    internal class TestRunner
    {
        public string MainDirectory { get; set; }
        public CompilationOptions CompilerOptions { get; set; }
        public string CustomLangaugeExtension { get; set; }
        public Logger TestLogger { get; set; }

        public static string TestRunnerIdentifier { get { return "TESTRUNNER"; } }

        public TestRunner(string mainDirectory, string customLanguageExtension, Logger logger)
        {
            MainDirectory = mainDirectory;
            CustomLangaugeExtension = String.Format(".{0}", customLanguageExtension);
            TestLogger = logger;
        }

        public void RunTests(CompilationOptions compilationOptions = 0)
        {
            var allLanguagesFolders = Directory.GetDirectories(MainDirectory);
            foreach (var languageFolder in allLanguagesFolders)
            {
                if (languageFolder.Length  == 0)
                {
                    continue;
                }

                var programs = Directory.GetFiles(languageFolder);
                var customLanguageFile = GetCustomLanguageFile(programs);
                var compiler = CompilerFactory.GetCompiler(programs[0], compilationOptions, customLanguageFile);

                TestLogger.LogLanguageName(languageFolder);

                if (customLanguageFile != null)
                {
                    programs = programs.Where(p => new FileInfo(p).Extension != CustomLangaugeExtension).ToArray();
                }

                Run(compiler, programs);
            }
        }

        private void Run(Compiler compiler, IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                TestLogger.WriteLineTestRunnerMessage(ConsoleColor.DarkYellow, "Compiling and running {0}", file);
                var output = Compile(compiler, file);
                TestLogger.WriteLineTestRunnerMessage(ConsoleColor.Green, "Output:");
                Console.Write(output);
                TestLogger.WriteLineTestRunnerMessage(ConsoleColor.DarkYellow, "Press any key to continue...\r\n");
                Console.ReadKey();
            }
        }

        private static string Compile(Compiler compiler, string file)
        {
            var compiledFile = compiler.Compile(file);
            var runResults = ExecutableRunner.RunExecutable(compiledFile);

            if (!String.IsNullOrEmpty(runResults.Error.ToString()))
            {
                
            }

            return runResults.Output.ToString();
        }

        private string GetCustomLanguageFile(IEnumerable<string> files)
        {
            return files.FirstOrDefault(f => new FileInfo(f).Extension == CustomLangaugeExtension);
        }
    }
}
