using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    interface ILanguageParser
    {
        bool IsTokenAllowed(string token);
        string GetNextToken(string source, int index);
        IEnumerable<string> GetTokens(string source);
    }
}
