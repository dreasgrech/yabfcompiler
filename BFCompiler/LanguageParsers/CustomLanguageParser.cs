
namespace YABFcompiler.LanguageParsers
{
    using System;

    public class CustomLanguageParser:Parser
    {
        public CustomLanguageParser(string[] languageDefinition) : base(Parse(languageDefinition))
        {
        }

        public static BiDictionaryOneToOne<string, LanguageInstruction> Parse(string[] languageDefinition)
        {
            var operators = new BiDictionaryOneToOne<string, LanguageInstruction>();
            foreach (var line in languageDefinition)
            {
                var firstSpace = line.IndexOf(" ");
                string dilInstruction = line.Substring(0, firstSpace),
                       languageToken = line.Substring(firstSpace + 1);

                operators.Add(languageToken, (LanguageInstruction)Enum.Parse(typeof(LanguageInstruction), dilInstruction));
            }

            return operators;
        }  
    }
}
