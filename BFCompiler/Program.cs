using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string bf = ">+++>+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++....",
                   ook = "Ook. Ook? Ook. Ook. Ook. Ook.";

            Parser bfParser = new BrainfuckParser(),
                   ookParser = new OokParser();

            TestParser(bf, bfParser);
            TestParser(ook, ookParser);

            var compiler = new Compiler(bfParser.GenerateDIL(bf));
            
            compiler.Compile("hello.bf");
            Console.ReadKey();
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
