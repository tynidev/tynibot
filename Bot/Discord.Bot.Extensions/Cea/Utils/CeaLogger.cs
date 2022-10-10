using PlayCEASharp.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Discord.Bot.Extensions.Cea.Utils
{
    internal class CeaLogger : ILogger
    {
        private Func<string, Task> log;

        internal CeaLogger(Func<string, Task> logger)
        {
            log = logger;
        }

        public void Log(string message)
        {
            log(message).Wait();
        }
    }
}
