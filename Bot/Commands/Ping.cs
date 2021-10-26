using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Ping : ModuleBase
    {
        [Command("ping"), Summary("Command to check if bot is running should respond with Pong!")]
        public async Task Pong()
        {
            await Context.Channel.SendMessageAsync("Pong!");
        }
    }
}
