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
    public class AdminAddTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, Guild guild, ISocketMessageChannel recruitingChannel, List<Team> teams)
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
            newPlayer.DiscordId = guildUser.Id;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), options["platform"].Value.ToString());
            newPlayer.PlatformId = options["id"].Value.ToString();

            if (newPlayer.Platform == Platform.Tracker && !Player.ValidateTrackerLink(newPlayer.PlatformId))
            {
                await command.FollowupAsync($"Your RL tracker link is invalid", ephemeral: true);
                return;
            }

            // Is player just updating tracker link? -> Update link
            (var team, var existingPlayer) = Team.FindPlayer(teams, guildUser);

            if (existingPlayer != null && !string.Equals(team.Name, teamName, StringComparison.InvariantCultureIgnoreCase))
            {
                await command.FollowupAsync($"Invalid use of add command. Please use the move command to change a user between teams", ephemeral: true);
                return;
            }
            
            if (team == null)
            {
                teamName = string.IsNullOrEmpty(teamName) ? "Free_Agents" : teamName;
                team = Team.AddPlayer(teams, teamName, newPlayer);
            }
            else
            {
                existingPlayer.Platform = newPlayer.Platform;
                existingPlayer.PlatformId = newPlayer.PlatformId;
            }

            await team.ConfigureTeamAsync(client, guild, recruitingChannel);
            await storageClient.SaveTableRow(Team.TableName, team.Name, guild.RowKey, team);
            await command.FollowupAsync($"{newPlayer.DiscordUser}'s RL tracker has been added to the recruiting board in channel <#{recruitingChannel.Id}>", ephemeral: true);
        }
    }
}
