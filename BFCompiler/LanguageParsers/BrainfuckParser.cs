
namespace YABFcompiler.LanguageParsers
{
    public class BrainfuckParser : Parser
    {
        public BrainfuckParser() : base(
            new BiDictionaryOneToOne<string, LanguageInstruction> { { ">", LanguageInstruction.IncPtr }, { "<", LanguageInstruction.DecPtr }, { "+", LanguageInstruction.Inc }, { "-", LanguageInstruction.Dec }, { ".", LanguageInstruction.Output }, { ",", LanguageInstruction.Input }, { "[", LanguageInstruction.StartLoop }, { "]", LanguageInstruction.EndLoop } })
        {

        }
    }
}