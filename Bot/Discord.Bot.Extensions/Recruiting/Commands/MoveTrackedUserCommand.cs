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
    // Todo: create and assign and delete roles for the teams, create/delete team channels as well.
    public class MoveTrackedUserCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
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
            await recruitingChannel.ModifyMessageAsync(oldTeam.MsgId, (message) => message.Content = oldTeam.ToMessage());

            var teamName = options["team"].Value.ToString();

            var newTeam = Team.FindTeam(teams, teamName);
            if (newTeam == null)
            {
                newTeam = new Team()
                {
                    Name = teamName,
                    Players = new List<Player>()
                };
            }

            newTeam.Players.Add(player);

            // If this is a captain make new team captain = player
            if (options.ContainsKey("captain") && (bool)options["captain"].Value)
            {
                newTeam.Captain = player;
            }

            // Update old team message
            if (oldTeam.Players.Count > 0)
            {
                await recruitingChannel.ModifyMessageAsync(oldTeam.MsgId, (message) => message.Content = oldTeam.ToMessage());
            }
            else
            {
                await recruitingChannel.DeleteMessageAsync(oldTeam.MsgId);
            }

            // Update new team message
            if(newTeam.MsgId == 0)
            {
                await recruitingChannel.SendMessageAsync(newTeam.ToMessage());
            }
            else
            {
                await recruitingChannel.ModifyMessageAsync(newTeam.MsgId, (message) => message.Content = newTeam.ToMessage());
            }

            await command.RespondAsync($"You have moved user {discordUser} from {oldTeam.Name} -> {newTeam.Name}", ephemeral: true);
            
        }
    }
}
