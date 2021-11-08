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

        public override bool IsGlobal => true;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            //{ 801598108467200031, new List<ApplicationCommandPermission>() } // tyni's server
        };

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string msg = $"Version: {version.ProductVersion}\n" +
                         $"Date: {ToDate(version.FileMinorPart ,version.ProductBuildPart, version.ProductPrivatePart)} UTC";

            foreach(var o in command.Data.Options.Where(o => true))
            {
                if(o.Name == "number_only" && (bool)o.Value)
                {
                    msg = $"{version.ProductVersion}";
                }
            }

            await command.RespondAsync(msg, ephemeral: true);
        }

        private static DateTime ToDate(int yyyy, int mmdd, int hhmm)
        {
            var date = new DateTime(yyyy, mmdd / 100, mmdd % 100);
            date = date.AddHours(hhmm / 100);
            date = date.AddSeconds(hhmm % 100);
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
