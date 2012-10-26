
namespace BFCompiler
{
    using System.Collections.Generic;

    interface ILanguageParser
    {
        bool IsTokenAllowed(string token);
        string GetNextToken(string source, int index);
        IEnumerable<string> GetTokens(string source);
    }
}
