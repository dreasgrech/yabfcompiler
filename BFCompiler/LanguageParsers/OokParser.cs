
namespace YABFcompiler.LanguageParsers
{
    public class OokParser : Parser
    {
        public OokParser() : base(
            new BiDictionaryOneToOne<string, LanguageInstruction> { { "Ook.Ook?", LanguageInstruction.IncPtr }, { "Ook?Ook.", LanguageInstruction.DecPtr }, { "Ook.Ook.", LanguageInstruction.Inc }, { "Ook!Ook!", LanguageInstruction.Dec }, { "Ook!Ook.", LanguageInstruction.Output }, { "Ook.Ook!", LanguageInstruction.Input }, { "Ook!Ook?", LanguageInstruction.StartLoop }, { "Ook?Ook!", LanguageInstruction.EndLoop } })
        {
;
        }
    }
}
