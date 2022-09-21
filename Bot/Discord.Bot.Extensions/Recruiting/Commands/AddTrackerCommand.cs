using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;
using Discord.Bot;
using System;
using TyniBot.Recruiting;
using Discord.Bot.Utils;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    public class AddTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, Guild guild, ISocketMessageChannel recruitingChannel, List<Team> teams)
        {
            var user = command.User as SocketGuildUser;

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = user.Nickname ?? user.Username;
            newPlayer.DiscordId = user.Id;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

            if (newPlayer.Platform == Platform.Tracker && !Player.ValidateTrackerLink(newPlayer.PlatformId))
            {
                await command.FollowupAsync($"Your RL tracker link is invalid", ephemeral: true);
                return;
            }

            // Is player just updating tracker link? -> Update link
            (var team, var existingPlayer) = Team.FindPlayer(teams, user);

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


            await team.ConfigureTeamAsync(client, guild, recruitingChannel);

            await storageClient.SaveTableRow(Team.TableName, team.Name, guild.RowKey, team);

            await command.FollowupAsync($"Your RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);
        }
    }
}
