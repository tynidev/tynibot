using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TyniBot.Models;

namespace TyniBot.Commands
{
    public class VersionSlashCommand : SlashCommand
    {
        public override string Name => "version";

        public override string Description => "Command to see what version of TyniBot is running.";

        public override bool DefaultPermissions => true;

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            await command.RespondAsync($"{ToDate(version.ProductBuildPart, version.ProductPrivatePart)} UTC");
        }

        private static DateTime ToDate(int days, int seconds)
        {
            var date = new DateTime(2000, 1, 1);
            date = date.AddDays(days);
            date = date.AddSeconds(seconds * 2);
            return date;
        }
    }
}
