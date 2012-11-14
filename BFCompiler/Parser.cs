
namespace YABFcompiler
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public abstract class Parser
    {
        public BiDictionaryOneToOne<string,LanguageInstruction> AllowedInstructions { get; private set; }
        protected int MinimumTokenLength { get; private set; }

        protected Parser(BiDictionaryOneToOne<string, LanguageInstruction> instructions)
        {
            AllowedInstructions = instructions;
            MinimumTokenLength = instructions.Min(d => d.Key.Length);
        }

        public IEnumerable<LanguageInstruction> GenerateDIL(string source)
        {
            return GetTokens(source).Select(token => AllowedInstructions.GetByFirst(token));
        }

        private IEnumerable<string> GetTokens(string source)
        {
            source = source.Replace("\r", "").Replace("\n", ""); 
            int index = 0;
            string token;
            while ((token = GetNextToken(source, ref index)) != null)
            {
                yield return token;
            }
        }

        private bool IsTokenAllowed(string token)
        {
            LanguageInstruction languageInstruction;
            return AllowedInstructions.TryGetByFirst(token, out languageInstruction);
        }

        private static string RemoveWhitespace(string s)
        {
            return Regex.Replace(s, "[ \t]", "");
        }

        private string GetNextToken(string source, ref int index)
        {
            var tokenLengthRange = MinimumTokenLength;
            while (index + tokenLengthRange <= source.Length)
            {
                string token = source.Substring(index, tokenLengthRange),
                    cleanToken = RemoveWhitespace(token);

                if (IsTokenAllowed(cleanToken))
                {
                    index += token.Length;
                    return cleanToken;
                }

                if (!AnyTokenStartsWith(token[0].ToString()))
                {
                    index++;
                    continue;
                }

                tokenLengthRange++;
            }

            return null;
        }

        private bool AnyTokenStartsWith(string beginning)
        {
            return AllowedInstructions.Any(i => i.Key.StartsWith(beginning));
        }
    }
}
