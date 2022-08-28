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
    public class AdminAddTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var guildUser = (SocketGuildUser)options["username"].Value;
            var teamName = String.Empty;

            if (options.TryGetValue("team", out SocketSlashCommandDataOption teamOption))
            {
                teamName = teamOption.Value.ToString();
            }

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = guildUser.Nickname ?? guildUser.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

            if (newPlayer.Platform == Platform.Tracker && !Player.ValidateTrackerLink(newPlayer.PlatformId))
            {
                await command.FollowupAsync($"Your RL tracker link is invalid", ephemeral: true);
                return;
            }

            // Is player just updating tracker link? -> Update link
            (var team, var existingPlayer) = Team.FindPlayer(teams, newPlayer.DiscordUser);

            if (existingPlayer != null && !string.IsNullOrEmpty(teamName))
            {
                await MoveTrackedUserCommand.Run(command, client, options, recruitingChannel, messages, teams);
                (team, existingPlayer) = Team.FindPlayer(teams, newPlayer.DiscordUser);
            }
            
            if (team == null || !string.IsNullOrEmpty(teamName))
            {
                teamName = string.IsNullOrEmpty(teamName) ? "Free_Agents" : teamName;
                team = Team.AddPlayer(teams, teamName, newPlayer);
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

            await command.FollowupAsync($"{newPlayer.DiscordUser}'s RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);
        }
    }
}
