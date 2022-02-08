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
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, string guildId, ISocketMessageChannel recruitingChannel, List<Team> teams)
        {
            var user = command.User as SocketGuildUser;

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = user.Nickname ?? user.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

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
            if(updatedTeam == null)
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
                updatedTeam.MsgId = (await recruitingChannel.SendMessageAsync(updatedTeam.ToMessage())).Id;
            }
            else
            {
                // This is an existing team -> Modify old team message
                await recruitingChannel.ModifyMessageAsync(updatedTeam.MsgId, (message) => message.Content = updatedTeam.ToMessage());
            }

            await command.RespondAsync($"Your RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);

            await storageClient.SaveTableRow(Team.TableName, updatedTeam.Name, guildId, updatedTeam);
        }
    }
}
