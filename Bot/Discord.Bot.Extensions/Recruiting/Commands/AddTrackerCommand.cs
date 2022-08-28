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
    public class AddTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var user = command.User as SocketGuildUser;

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = user.Nickname ?? user.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

            if (newPlayer.Platform == Platform.Tracker && !Player.ValidateTrackerLink(newPlayer.PlatformId))
            {
                await command.FollowupAsync($"Your RL tracker link is invalid", ephemeral: true);
                return;
            }

            // Is player just updating tracker link? -> Update link
            (var team, var existingPlayer) = Team.FindPlayer(teams, newPlayer.DiscordUser);

            // Is player not on a team? -> Add to FreeAgents
            if (team == null)
            {
                team = Team.AddPlayer(teams, "Free_Agents", newPlayer);
            }
            else
            {
                existingPlayer.Platform = newPlayer.Platform;
                existingPlayer.PlatformId = newPlayer.PlatformId;
            }

            // Have we added this team message yet? -> Write team message and move to next team
            if (team.MsgId == 0)
            {
                await recruitingChannel.SendMessageAsync(team.ToMessage());                
            }
            else
            {
                // This is an existing team -> Modify old team message
                await recruitingChannel.ModifyMessageAsync(team.MsgId, (message) => message.Content = team.ToMessage());
            }            

            await command.FollowupAsync($"Your RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);
        }
    }
}
