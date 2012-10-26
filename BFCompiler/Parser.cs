using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    abstract class Parser
    {
        protected Dictionary<string,DILInstruction> AllowedInstructions { get; private set; }
        protected int TokenLength { get; private set; }

        protected Parser(Dictionary<string,DILInstruction> instructions, int tokenLength)
        {
            AllowedInstructions = instructions;
            TokenLength = tokenLength;
        }

        public IEnumerable<string> GetTokens(string source)
        {
            int index = 0;
            string token;
            while ((token = GetNextToken(source, ref index)) != null)
            {
                index = IncrementCounterForNextToken(index);
                yield return token;
            }
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
            while (index + TokenLength <= source.Length)
            {
                string token = source.Substring(index, TokenLength);
                if (IsTokenAllowed(token))
                {
                    return token;
                }

                index++;
            }

            return null;
        }

        private int IncrementCounterForNextToken(int counter)
        {
            //return counter + 1;
            return counter + TokenLength; // this will skip over a chunk, making parser faster but it requires that all the tokens are of a fixed length 
        }
    }
}
