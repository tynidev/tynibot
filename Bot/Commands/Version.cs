using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Version : ModuleBase
    {
        [Command("version"), Summary("Command to see what version of TyniBot is running.")]
        public async Task VersionCommand()
        {
            await Context.Channel.SendMessageAsync($"{typeof(TyniBot.TynibotHost).Assembly.GetName().Version.ToString()}");
        }
    }
}
