using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;
using Discord.Bot;
using Discord.Bot.Utils;
using System;
using TyniBot.Recruiting;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    public class DeleteTeamTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, Guild guild, ISocketMessageChannel recruitingChannel, List<Team> teams)
        {
            var teamName = options["team"].Value.ToString();

            // Player not exist? -> respond with error
            var team = Team.FindTeam(teams, teamName);
            if (team == null)
            {
                await command.FollowupAsync($"Team {teamName} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            // Remove old team message
            await team.CleanupDeletedTeamAsync(client, guild, recruitingChannel);
            await storageClient.DeleteTableRow(Team.TableName, team.Name, guild.RowKey);
            await command.FollowupAsync($"You have removed team {teamName}", ephemeral: true);
        }
    }
}
