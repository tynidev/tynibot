using Discord.Bot;
using Discord.Bot.Utils;
using Discord.WebSocket;
using PlayCEASharp.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public abstract class CeaUserCommand : UserCommand
    {
        public override bool DefaultPermissions => true;

        public override bool IsGlobal => true;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => GuildIdMappings.defaultSlashCommandPermissions;

        public override async Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient)
        {
            List<Team> teams = TeamResolver.ResolveUsersTeam(command.Data.Member);
            if (teams == null)
            {
                await command.RespondAsync("That user is not on a team.", ephemeral: true);
                return;
            }

            await HandleCommandAsync(command, client, storageClient, teams);
        }

        public abstract Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient, List<Team> teams);

        public override ApplicationCommandProperties Build()
        {
            var builder = new UserCommandBuilder()
                    .WithName(this.Name)
                    .WithDefaultPermission(this.DefaultPermissions);

            return builder.Build();
        }
    }
}
