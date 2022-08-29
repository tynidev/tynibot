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

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, discordUser);
            if (player == null)
            {
                await command.FollowupAsync($"User {discordUser} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            // If player was captain of old team remove that teams captain
            if (oldTeam.Captain?.DiscordUser == player.DiscordUser)
                oldTeam.Captain = null;

            // Move Player
            oldTeam.Players.Remove(player);

            var teamName = options["team"].Value.ToString();

            var newTeam = Team.FindTeam(teams, teamName);
            bool isNewTeam = false;
            if (newTeam == null)
            {
                isNewTeam = true;
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
                newTeam.MsgId = (await recruitingChannel.SendMessageAsync(newTeam.ToMessage())).Id;
            }
            else
            {
                await recruitingChannel.ModifyMessageAsync(newTeam.MsgId, (message) => message.Content = newTeam.ToMessage());
            }

            var transactions = new List<(string, TableTransactionActionType, Team, ETag)>();
            if (oldTeam.Players.Count > 0)
            {
                transactions.Add((oldTeam.Name, TableTransactionActionType.UpdateMerge, oldTeam, oldTeam.etag));
            }
            else
            {
                transactions.Add((oldTeam.Name, TableTransactionActionType.Delete, null, oldTeam.etag));
            }

            transactions.Add((newTeam.Name, isNewTeam ? TableTransactionActionType.UpsertMerge : TableTransactionActionType.UpdateMerge, newTeam, newTeam.etag));

            // if the transaction fails it should retry, and then the message will be updated to reflect the actual value in storage.
            await storageClient.ExecuteTransaction(Team.TableName, transactions, guild.RowKey);
            
            await command.FollowupAsync($"You have moved user {discordUser} from {oldTeam.Name} -> {newTeam.Name}", ephemeral: true);
        }
    }
}
