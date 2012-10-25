using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    abstract class Parser
    {
        protected string[] AllowedTokens { get; private set; }
        protected int TokenLength { get; private set; }

        protected Parser(string[] allowedTokens, int tokenLength)
        {
            AllowedTokens = allowedTokens;
            TokenLength = tokenLength;
        }

        public bool IsTokenAllowed(string token)
        {
            return AllowedTokens.Contains(token);
        }

        public string GetNextToken(string source, ref int index)
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

        private int IncrementCounterForNextToken(int counter)
        {
            //return counter + 1;
            return counter + TokenLength; // this will skip over a chunk, making parser faster but it requires that all the tokens are of a fixed length 
        }
    }
}
