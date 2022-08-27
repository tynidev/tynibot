using Discord;
using Discord.Bot;
using Discord.Bot.Utils;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot.Recruiting;

namespace TyniBot.Commands
{
    public class RecruitingCommand : SlashCommand
    {
        public override string Name => "recruiting";

        public override string Description => "Add your RL tracker for the next season of CEA!";

        public override bool DefaultPermissions => false;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => GuildIdMappings.recruitingPermissions;

        public override bool IsGlobal => false;

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Guild guild)
        {
            var channel = command.Channel as SocketGuildChannel;
            var subCommand = command.Data.Options.First();
            var options = subCommand.Options.ToDictionary(o => o.Name, o => o);

            await command.RespondAsync($"Starting Command {command.CommandName} {subCommand.Name}", ephemeral: true);

            // Get all messages in channel
            var recruitingChannel = await client.GetChannelAsync(guild.RecruitingChannelId) as ISocketMessageChannel;

            // cache these values eventually as well to improve performance
            var teams = await storageClient.GetAllRowsAsync<Team>(Team.TableName, guild.ToString()); 

            if (teams.Count == 0)
            {
                var messages = await GetAllChannelMessages(recruitingChannel);

                // Parse messages into teams
                teams = ParseMessageAsync(messages);

                if (teams.Count > 0)
                {
                    await ConvertMessageTeamsToStorage(teams, guild.RowKey, storageClient);
                }
            }

            switch (subCommand.Name)
            {
                case "add":
                    await AddTrackerCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                case "adminadd":
                    await AdminAddTrackerCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                case "move":
                    await MoveTrackedUserCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                case "remove":
                    await RemoveTrackedUserCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                case "deleteteam":
                    await DeleteTeamTrackerCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                case "lookingforplayers":
                    await LookingForPlayersCommand.Run(command, client, storageClient, options, guild, recruitingChannel, teams);
                    break;
                default:
                    await command.FollowupAsync($"SubCommand {subCommand} not supported", ephemeral: true);
                    return;
            }
        }

        public override SlashCommandProperties Build()
        {
            var addCmd = new SlashCommandOptionBuilder()
            {
                Name = "add",
                Description = "Add your RL tracker for the next season of CEA!",
                Type = ApplicationCommandOptionType.SubCommand
            };
            addCmd.AddOption("platform",
                                ApplicationCommandOptionType.String,
                                "Platorm you play on",
                                isRequired: true,
                                choices:
                                    new ApplicationCommandOptionChoiceProperties[] { new ApplicationCommandOptionChoiceProperties() { Name = "epic", Value = "Epic" },
                                        new ApplicationCommandOptionChoiceProperties() { Name = "steam", Value = "Steam" },
                                        new ApplicationCommandOptionChoiceProperties() { Name = "playstation", Value = "Playstation" },
                                        new ApplicationCommandOptionChoiceProperties() { Name = "xbox", Value = "Xbox" },
                                        new ApplicationCommandOptionChoiceProperties() { Name = "tracker", Value = "Tracker" }
                                    });
            addCmd.AddOption("id", ApplicationCommandOptionType.String, "For steam use your id, others use username, tracker post full tracker", isRequired: true);

           
            var builder = new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOption(addCmd);

            return builder.Build();
        }

        internal List<Team> ParseMessageAsync(List<IMessage> messages)
        {
            var teams = new List<Team>();

            foreach (var teamMsg in messages)
            {
                try
                {
                    teams.Add(Team.ParseTeam(teamMsg.Id, teamMsg.Content));
                }
                catch
                {
                }
            }

            return teams;
        }

        internal async Task<List<IMessage>> GetAllChannelMessages(ISocketMessageChannel channel, int limit = 100)
        {
            var msgs = new List<IMessage>();
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            while (messages.Any() && msgs.Count < limit)
            {
                msgs.AddRange(messages);
                messages = await channel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();
            }

            return msgs;
        }

        internal async Task ConvertMessageTeamsToStorage(List<Team> teams, string rowKey, StorageClient storageClient)
        {
            List<(string, Team)> rowKeysAndTeams = new List<(string, Team)>();

            foreach (Team team in teams)
            {
                rowKeysAndTeams.Add((team.Name, team));
            }

            await storageClient.SaveTableRows(Team.TableName, rowKeysAndTeams, rowKey);
        }

    }
}
