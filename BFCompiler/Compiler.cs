
namespace YABFcompiler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using DIL;
    using EventArguments;
    using Exceptions;

    /*
     * NOTE: All of the below optimizations are heavily outdated.
     * Since the time when they were written, I've completely rearchitectured the 
     * optimizition process with the introduction of the DIL (Dreas Intermediate Language).
     * 
     * When I'll have a good working state of the compiler, I'll rewrite all of the 
     * documentation for the new optimization process.
     * 
     * 
     * 
     * 
     * 
     * ASSUMPTIONS
     * Assumption #1:
     *  Every cell is initialized with 0
     * 
     * OPTIMIZATIONS
     * Optimization #1:
     *  Loops which could never be entered are ignored.
     *  This can happen when either:
     *      1) A loop starts immediately after another loop or
     *      2) The loop is at the beginning of the program.
     *  
     * Optimization #2:
     *  Sequences of Input and Output are grouped in a for-loop
     *  
     * NOTE: I'm not sure how beneficial this optimization is because although it can reduce the 
     * size of the compiled file, it may degrade performance due to the increased jmps introduced by
     * the loop.
     * 
     * Optimization #3:
     * This optimization groups together sequences of Incs and Decs, and IncPtrs and DecPtrs
     * Examples: 
     *      ++--+ is grouped as a single Inc(1) and -+-- is grouped as a single Dec(2)
     *      +++--- is eliminated since it's basically a noop
     *      ><<><< is grouped as a single DecPtr(2) and >><>> is grouped as a single IncPtr(3)
     *      >>><<< is eliminated since it's basically a noop
     *      
     * Optimization #4:
     * Some patterns of clearance loops are detected and replaced with Assign(0)
     * Examples:
     *      [-], [+]
     *      
     * Optimization #5:
     * Simple loop code walking.
     * 
     * A simple loop doesn't contain any input or out, and it also doesn't contain nested loop.
     * A simple loop also returns to the starting position after execution.  Meaning that the position of StartLoop is equal to the position of EndLoop.
     * 
     * These simple loops are replaced with multiplication operations.
     * 
     * So for ++[>+++<-] the emitted code will be:
     * 
     *     chArray[index] = (char) (chArray[index] + '\x0002');
     *     chArray[index + 1] = (char) (chArray[index + 1] + ((char) (chArray[index] * '\x0003')));
     *     chArray[index] = '\0';
     * 
     * Notice how the loop in brainfuck was replaced by a multiplication operation and an assigment, 
     * the assignment being the NULL to the starting position of the loop since that's the 
     * reason why the loop was halted.
     */

    public class Compiler
    {
        /// <summary>
        /// The size of the array domain for brainfuck to work in
        /// 
        /// TODO: Maybe this should be passed as a command line arg?
        /// </summary>
        private const int DomainSize = 0x493e0;

        public Parser Parser { get; private set; }
        public CompilationOptions Options { get; private set; }

        public event EventHandler<CompilationWarningEventArgs> OnWarning;

        public Compiler(Parser parser, CompilationOptions options = 0)
        {
            Parser = parser;
            Options = options;
        }

        /// <summary>
        /// Returns the location of the compiled assembly
        /// </summary>
        /// <returns></returns>
        public string Compile(string filename)
        {
            var instructions = Parser.GenerateDIL(File.ReadAllText(filename)).ToArray();

            LanguageInstruction? areLoopOperationsBalanced;
            if ((areLoopOperationsBalanced = AreLoopOperationsBalanced(instructions)) != null)
            {
                throw new InstructionNotFoundException(String.Format("Expected to find an {0} instruction but didn't.", (~areLoopOperationsBalanced.Value).ToString()));
            }

            var dilInstructions = new DILOperationSet(instructions);

            // If we're not in debug mode, optimize the shit out of it!
            if (!OptionEnabled(CompilationOptions.DebugMode))
            {
                while (dilInstructions.Optimize(ref dilInstructions))
                {
                }
            }

            //var interpreter = new Interpreter(30000);
            //interpreter.Run(dilInstructions);

            return CompileToExecutable(dilInstructions, filename);
        }

        /// <summary>
        /// Returns null if the number of StartLoop operations match the number of EndLoop operations
        /// 
        /// Otherwise, it returns the operation with the excess total.
        /// </summary>
        private LanguageInstruction? AreLoopOperationsBalanced(LanguageInstruction[] instructions)
        {
            int totalStartLoopOperations = instructions.Where(instruction => instruction == LanguageInstruction.StartLoop).Count(),
                totalEndLoopOperations = instructions.Where(instruction => instruction == LanguageInstruction.EndLoop).Count();

            if (totalStartLoopOperations == totalEndLoopOperations)
            {
                return null;
            }

            if (totalStartLoopOperations > totalEndLoopOperations)
            {
                return LanguageInstruction.StartLoop;
            }

            return LanguageInstruction.EndLoop;
        }

        /// <summary>
        /// TODO: https://github.com/dreasgrech/yabfcompiler/issues/11
        ///       Do not emit the array and the pointer variables if they're not needed.
        /// </summary>
        private string CompileToExecutable(DILOperationSet operations, string filename)
        {
            var assembly = CreateAssemblyAndEntryPoint(filename);
            ILGenerator ilg = assembly.MainMethod.GetILGenerator();

            LocalBuilder array = ilg.CreateArray<byte>(DomainSize);
            LocalBuilder ptr = null;

            // Do not emit the pointer variable if it's not needed.
            if (operations.ContainsPointerOperations())
            {
                ptr = ilg.DeclareIntegerVariable();
            }

            foreach (var dilInstruction in operations)
            {
                dilInstruction.Emit(ilg, array, ptr);
            }

            ilg.Emit(OpCodes.Ret);

            Type t = assembly.MainClass.CreateType();
            assembly.DynamicAssembly.SetEntryPoint(assembly.MainMethod, PEFileKinds.ConsoleApplication);

            var compiledAssemblyFile = String.Format("{0}.exe", Path.GetFileNameWithoutExtension(filename));
            assembly.DynamicAssembly.Save(compiledAssemblyFile);

            return compiledAssemblyFile;
        }

        private AssemblyInfo CreateAssemblyAndEntryPoint(string filename)
        {
            var fileInfo = new FileInfo(filename);
            AssemblyName an = new AssemblyName { Name = fileInfo.Name };
            AppDomain ad = AppDomain.CurrentDomain;
            AssemblyBuilder ab = ad.DefineDynamicAssembly(an, AssemblyBuilderAccess.Save);

            ModuleBuilder mb = ab.DefineDynamicModule(an.Name, String.Format("{0}.exe", Path.GetFileNameWithoutExtension(filename)), true);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder fb = tb.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig, null, null);

            return new AssemblyInfo(ab, tb, fb);
        }

        private bool OptionEnabled(CompilationOptions option)
        {
            return (Options & option) == option;
        }
    }
}