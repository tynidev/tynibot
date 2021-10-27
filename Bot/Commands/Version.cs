using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Version : ModuleBase
    {
        [Command("version"), Summary("Command to see what version of TyniBot is running.")]
        public async Task VersionCommand()
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            await Context.Channel.SendMessageAsync($"{version.ProductVersion.ToString()}");
        }
    }
}
