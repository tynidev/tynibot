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
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, string guildId, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var user = command.User as SocketGuildUser;

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = user.Nickname ?? user.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

            // Is player just updating tracker link? -> Update link
            bool updated = false;
            foreach(var team in teams)
            {
                var exists = team.Players.Where((p) => p.DiscordUser == newPlayer.DiscordUser);
                if (exists.Any())
                {
                    // Player exists on team so just update
                    var existingPlayer = exists.First();
                    existingPlayer.Platform = newPlayer.Platform;
                    existingPlayer.PlatformId = newPlayer.PlatformId;
                    updated = true;
                    break;
                }
            }

            // Is player not on a team? -> Add to FreeAgents
            if(!updated)
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
            }

            List<(string, Team)> rowkeysAndTeams = new List<(string, Team)>();

            foreach(var team in teams)
            {
                rowkeysAndTeams.Add((team.Name, team));
                // Have we added this team message yet? -> Write team message and move to next team
                if(team.MsgId == 0)
                {
                    await recruitingChannel.SendMessageAsync(team.ToMessage());
                    continue;
                }

                // This is an existing team -> Modify old team message
                await recruitingChannel.ModifyMessageAsync(team.MsgId, (message) => message.Content = team.ToMessage());
            }

            await command.RespondAsync($"Your RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);

            await storageClient.SaveTableRows(Team.TableName, rowkeysAndTeams, guildId);
        }
    }
}
