
namespace YABFcompiler
{
    using System;
    using System.IO;
    using Exceptions;
    using LanguageParsers;

    public static class CompilerFactory
    {
        public static Compiler GetCompiler(string filename, CompilationOptions options = 0, string customLanguageFile = "")
        {
            var fileInfo = new FileInfo(filename);
            Parser parser;

            if (!String.IsNullOrEmpty(customLanguageFile))
            {
                parser = new CustomLanguageParser(File.ReadAllLines(customLanguageFile));
            }
            else
            {
                switch (fileInfo.Extension.Substring(1).ToLower()) // remove the period.
                {
                    case "bf": parser = new BrainfuckParser(); break;
                    case "ook": parser = new OokParser(); break;
                    case "sook": parser = new ShortOokParser(); break;
                    default: throw new UnknownLanguageException();
                }
            }

            return new Compiler(parser, options);
        }
    }
}
