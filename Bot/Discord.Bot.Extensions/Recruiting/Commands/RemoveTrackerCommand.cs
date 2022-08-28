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
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var guildUser = (SocketGuildUser)options["username"].Value;
            var discordUser = guildUser.Nickname ?? guildUser.Username;

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, discordUser);
            if (player == null)
            {
                await command.FollowupAsync($"User {discordUser} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            oldTeam.RemovePlayer(player);

            // Update old team message
            if (oldTeam.Players.Count > 0)
            {
                await recruitingChannel.ModifyMessageAsync(oldTeam.MsgId, (message) => message.Content = oldTeam.ToMessage());
            }
            else
            {
                await recruitingChannel.DeleteMessageAsync(oldTeam.MsgId);
            }

            await command.FollowupAsync($"You have removed user {discordUser} from {oldTeam.Name}", ephemeral: true);
        }
    }
}
