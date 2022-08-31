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
    public class RemoveTrackedUserCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, Guild guild, ISocketMessageChannel recruitingChannel, List<Team> teams)
        {
            var guildUser = (SocketGuildUser)options["username"].Value;
            var discordUser = guildUser.Nickname ?? guildUser.Username;

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, guildUser);
            if (player == null)
            {
                await command.FollowupAsync($"User {guildUser.Nickname ?? guildUser.Username} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            await oldTeam.RemovePlayerAsync(player, guildUser);

            // Update old team message
            await oldTeam.ConfigureTeamAsync(client, guild, recruitingChannel);

            if (oldTeam.Players.Count > 0)
            {
                await storageClient.SaveTableRow(Team.TableName, oldTeam.Name, guild.RowKey, oldTeam);
            }
            else
            {
                await storageClient.DeleteTableRow(Team.TableName, oldTeam.Name, guild.RowKey);
            }

            await command.FollowupAsync($"You have removed user {discordUser} from {oldTeam.Name}", ephemeral: true);
        }
    }
}
