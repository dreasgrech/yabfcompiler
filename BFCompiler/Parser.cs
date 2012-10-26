
namespace BFCompiler
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    abstract class Parser
    {
        protected Dictionary<string,DILInstruction> AllowedInstructions { get; private set; }
        protected int MinimumTokenLength { get; private set; }

        protected Parser(Dictionary<string,DILInstruction> instructions)
        {
            AllowedInstructions = instructions;
            MinimumTokenLength = instructions.Min(d => d.Key.Length);
        }

        public IEnumerable<string> GetTokens(string source)
        {
            source = source.Replace("\r\n", "");
            int index = 0;
            string token;
            while ((token = GetNextToken(source, ref index)) != null)
            {
                yield return token;
            }
        }

        private static string RemoveWhitespace(string s)
        {
            return Regex.Replace(s, "[ \t]", "");
        }

        public IEnumerable<DILInstruction> GenerateDIL(string source)
        {
            return GetTokens(source).Select(token => AllowedInstructions[token]);
        }

        private bool IsTokenAllowed(string token)
        {
            return AllowedInstructions.ContainsKey(token);
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

                tokenLengthRange++;
            }

            return null;
        }
    }
}
