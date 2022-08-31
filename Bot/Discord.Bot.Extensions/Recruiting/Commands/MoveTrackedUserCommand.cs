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
using Azure.Data.Tables;
using Azure;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    // Todo: create and assign and delete roles for the teams, create/delete team channels as well.
    public class MoveTrackedUserCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Dictionary<string, SocketSlashCommandDataOption> options, Guild guild, ISocketMessageChannel recruitingChannel, List<Team> teams)
        {
            var guildUser = (SocketGuildUser)options["username"].Value;
            var discordUser = guildUser.Nickname ?? guildUser.Username;
            var teamName = options["team"].Value.ToString();
            var captain = options.ContainsKey("captain") && (bool)options["captain"].Value;

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, guildUser);
            if (player == null)
            {
                await command.FollowupAsync($"User {discordUser} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            await oldTeam.RemovePlayerAsync(player, guildUser);
            var newTeam = Team.AddPlayer(teams, teamName, player, captain);
            bool isNewTeam = newTeam.MsgId == 0;

            // Update old team message
            await oldTeam.ConfigureTeamAsync(client, guild, recruitingChannel);

            // Update new team message
            await newTeam.ConfigureTeamAsync(client, guild, recruitingChannel);

            var transactions = new List<(string, TableTransactionActionType, Team, ETag)>();
            if (oldTeam.Players.Count > 0)
            {
                transactions.Add((oldTeam.Name, TableTransactionActionType.UpdateMerge, oldTeam, oldTeam.etag));
            }
            else
            {
                transactions.Add((oldTeam.Name, TableTransactionActionType.Delete, null, oldTeam.etag));
            }

            if (!string.Equals(newTeam.Name, oldTeam.Name, StringComparison.OrdinalIgnoreCase))
            {
                transactions.Add((newTeam.Name, isNewTeam ? TableTransactionActionType.UpsertMerge : TableTransactionActionType.UpdateMerge, newTeam, newTeam.etag));
            }

            // if the transaction fails it should retry, and then the message will be updated to reflect the actual value in storage.
            await storageClient.ExecuteTransaction(Team.TableName, transactions, guild.RowKey);
            
            await command.FollowupAsync($"You have moved user {discordUser} from {oldTeam.Name} -> {newTeam.Name}", ephemeral: true);
        }
    }
}
