using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord.Bot;
using System.Linq;
using Discord.Rest;

namespace TyniBot.Commands
{
    public class VersionSlashCommand : SlashCommand
    {
        public override string Name => "version";

        public override string Description => "Command to see what version of TyniBot is running.";

        public override bool DefaultPermissions => true;

        public override async Task HandleCommandAsync(SocketSlashCommand command)
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string msg = $"Version: {version.ProductVersion}\n" +
                         $"Date: {ToDate(version.ProductBuildPart, version.ProductPrivatePart)} UTC";

            foreach(var o in command.Data.Options.Where(o => true))
            {
                if(o.Name == "number_only" && (bool)o.Value)
                {
                    msg = $"{version.ProductVersion}";
                }
            }

            await command.RespondAsync(msg);
        }

        private static DateTime ToDate(int days, int seconds)
        {
            var date = new DateTime(2000, 1, 1);
            date = date.AddDays(days);
            date = date.AddSeconds(seconds * 2);
            return date;
        }

        public override ApplicationCommandProperties Build()
        {

            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);

            builder.AddOption(
                name: "number_only",
                type: ApplicationCommandOptionType.Boolean,
                description: "Outputs only the version number");

            return builder.Build();
        }
    }
}
