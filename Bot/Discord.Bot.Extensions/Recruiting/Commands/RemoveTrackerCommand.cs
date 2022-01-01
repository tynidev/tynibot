using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;
using Discord.Bot;
using System;
using TyniBot.Recruiting;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    public class RemoveTrackedUserCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, string guildId, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var guildUser = (SocketGuildUser)options["username"].Value;
            var discordUser = guildUser.Nickname ?? guildUser.Username;

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, discordUser);
            if (player == null)
            {
                await command.RespondAsync($"User {discordUser} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            // If player was captain of old team remove that teams captain
            if (oldTeam.Captain?.DiscordUser == player.DiscordUser)
                oldTeam.Captain = null;

            // Move Player
            oldTeam.Players.Remove(player);

            // Update old team message
            if (oldTeam.Players.Count > 0)
            {
                await recruitingChannel.ModifyMessageAsync(oldTeam.MsgId, (message) => message.Content = oldTeam.ToMessage());
                await storageClient.SaveTableRow(Team.TableName, oldTeam.Name, guildId, oldTeam);
            }
            else
            {
                await recruitingChannel.DeleteMessageAsync(oldTeam.MsgId);
                await storageClient.DeleteTableRow(Team.TableName, oldTeam.Name, guildId);
            }

            await command.RespondAsync($"You have removed user {discordUser} from {oldTeam.Name}", ephemeral: true);
        }
    }
}
