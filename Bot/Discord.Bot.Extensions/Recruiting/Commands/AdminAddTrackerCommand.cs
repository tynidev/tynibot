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

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = guildUser.Nickname ?? guildUser.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();


            if (newPlayer.Platform == Platform.Tracker && !Player.ValidateTrackerLink(newPlayer.PlatformId))
            {
                await command.RespondAsync($"Your RL tracker link is invalid", ephemeral: true);
                return;
            }

            // Is player just updating tracker link? -> Update link
            Team updatedTeam = null;
            foreach (var team in teams)
            {
                var exists = team.Players.Where((p) => p.DiscordUser == newPlayer.DiscordUser);
                if (exists.Any())
                {
                    // Player exists on team so just update
                    var existingPlayer = exists.First();
                    existingPlayer.Platform = newPlayer.Platform;
                    existingPlayer.PlatformId = newPlayer.PlatformId;
                    updatedTeam = team;
                    break;
                }
            }

            // Is player not on a team? -> Add to FreeAgents
            if (updatedTeam == null)
            {
                var freeAgents = Team.FindTeam(teams, "Free_Agents");

                // Not found? -> Add Free Agent team
                if (freeAgents == null)
                {
                    freeAgents = new Team()
                    {
                        Name = "Free_Agents",
                        Players = new List<Player>()
                    };
                    teams.Add(freeAgents);
                }

                freeAgents.Players.Add(newPlayer);
                updatedTeam = freeAgents;
            }

            // Have we added this team message yet? -> Write team message and move to next team
            if (updatedTeam.MsgId == 0)
            {
                await recruitingChannel.SendMessageAsync(updatedTeam.ToMessage());
            }
            else
            {
                // This is an existing team -> Modify old team message
                await recruitingChannel.ModifyMessageAsync(updatedTeam.MsgId, (message) => message.Content = updatedTeam.ToMessage());
            }            

            await command.RespondAsync($"{newPlayer.DiscordUser}'s RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);
        }
    }
}
