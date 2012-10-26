using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BFCompiler.CommandLineArgs;
using BFCompiler.Exceptions;

namespace BFCompiler
{
    class Program
    {
        private static OptionSet options;

        private static string option_filename;

        static void Main(string[] args)
        {
            if (!HandleCommandLineArgs(args))
            {
                ShowHelp(options);
                return;
            }

            string bf = "+ ++ +++ ++++ +>+>> >>++++ +++++++ ++++++++ +++++++++ ++++++++++ ++++++>++++ ++++++++++++ +++++++++++++ +++<<<<<<[>[>> >>>>+>+<<<<<<<- ]>>>>>>>[<<<<<<< +>>>>>>>-]<[>++++ ++++++[-<-[>>+>+<< <-]>>>[<<<+>>>-]+<[ >[-]<[-]]>[<<[>>>+<< <-]>>[-]]<<]>>>[>>+>+ <<<-]>>>[<<<+>>>-]+<[> [-]<[-]]>[<<+>>[-]]<<<< <<<]>>>>>[++++++++++++++ +++++++++++++++++++++++++ +++++++++.[-]]++++++++++<[ ->-<]>+++++++++++++++++++++ +++++++++++++++++++++++++++.  [-]<<<<<<<<<<<<[>>>+>+<<<<-]> >>>[<<<<+>>>>-]<-[>>.>.<<<[-]] <<[>>+>+<<<-]>>>[<<<+>>>-]<<[<+ >-]>[<+>-]<<<-]",
                   ook = "Ook. Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook! Ook?  Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook? Ook! Ook!  Ook? Ook! Ook? Ook. Ook! Ook. Ook. Ook? Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook! Ook? Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook? Ook! Ook! Ook? Ook! Ook? Ook. Ook. Ook.  Ook! Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook! Ook. Ook! Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook! Ook. Ook. Ook? Ook. Ook? Ook.  Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook! Ook? Ook? Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook? Ook! Ook!  Ook? Ook! Ook? Ook. Ook! Ook. Ook. Ook? Ook. Ook? Ook.  Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook!  Ook? Ook? Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook.  Ook? Ook! Ook! Ook? Ook! Ook? Ook. Ook! Ook! Ook! Ook!  Ook! Ook! Ook! Ook. Ook? Ook. Ook? Ook. Ook? Ook. Ook?  Ook. Ook! Ook. Ook. Ook. Ook. Ook. Ook. Ook. Ook! Ook.  Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook!  Ook! Ook! Ook. Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook!  Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook! Ook. Ook.  Ook? Ook. Ook? Ook. Ook. Ook! Ook. ";

            var compiler = GetCompiler(option_filename);

            compiler.Compile(Path.GetFileNameWithoutExtension(option_filename));
        }

        private static Compiler GetCompiler(string filename)
        {
            var code = ReadFile(filename);
            var fileInfo = new FileInfo(filename);
            Parser parser;

            switch (fileInfo.Extension.Substring(1)) // remove the period.
            {
                case "bf": parser = new BrainfuckParser(); break;
                case "ook": parser = new OokParser(); break;
                default:throw new UnknownLanguageException();
            }

            return new Compiler(parser.GenerateDIL(code));

        }

        private static void ShowHelp(OptionSet optionSet)
        {
            

        }

        private static string ReadFile(string file)
        {
            return File.ReadAllText(file);
        }

        private static bool HandleCommandLineArgs(string[] args)
        {
            bool status = true;
            options = new OptionSet
                          {
                              /*
                               * {"m|mutation=", "The mutation rate (0-1)", (double v) => mutationRate = v},
                              {"ctype=", "The crossover type [one | two ]", v => crossoverType = v},
                               */
                              {"?|h|help", "Show help", v => { status = false; }},
                              {"<>", v => option_filename = v}
                          };
            try
            {
                options.Parse(args);
            }
            catch (OptionException ex)
            {
                status = false;
            }

            if (String.IsNullOrEmpty(option_filename))
            {
                status = false;
            }

            return status;

        }

        static void TestParser(string source, Parser parser)
        {
            Console.WriteLine("Source: {0}", source);
            foreach (var token in parser.GenerateDIL(source))
            {
                Console.WriteLine("{0}", token);
            }

            Console.WriteLine("---");
        }
    }
}
