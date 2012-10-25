using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BFCompiler
{
    class OokParser : Parser
    {
        public OokParser() : base(new[] { "Ook. Ook?", "Ook? Ook.", "Ook. Ook.", "Ook! Ook!", "Ook! Ook.", "Ook. Ook!", "Ook! Ook?", "Ook? Ook!" }, 9)
        {
        }
    }
}
