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
    public class AddTrackerCommand : SlashCommand
    {
        public override string Name => "addtracker";

        public override string Description => "Add your RL tracker for recruiting purposes!";

        public override bool DefaultPermissions => false;

        public override bool IsGlobal => false;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>();

        private static readonly ImmutableDictionary<ulong, ulong> recruitingChannelForGuild = new Dictionary<ulong, ulong> {
            { 902581441727197195, 903521423522398278}, //tynibot test
            { 598569589512863764,  541894310258278400} //msft rl
        }.ToImmutableDictionary();

        public AddTrackerCommand()
            : base()
        {
            this.GuildIdsAndPermissions.Add(902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) }); // tynibot test
            //this.GuildIdsAndPermissions.Add(124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(598569589512863764, ApplicationCommandPermissionTarget.Role, true) }); // msft rl
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var channel = command.Channel as SocketGuildChannel;

            if (!recruitingChannelForGuild.TryGetValue(channel.Guild.Id, out var recruitingChannelId)) {
                await command.RespondAsync("Channel is not part of a guild that supports recruiting", ephemeral: true);
                return;
            }


            var user = command.User as SocketGuildUser;

            // Construct new player from parameters
            var newPlayer = new Player();
            newPlayer.DiscordUser = user.Nickname ?? user.Username;
            newPlayer.Platform = (Platform)Enum.Parse(typeof(Platform), command.Data.Options.Where(o => string.Equals(o.Name, "platform")).First().Value.ToString());
            newPlayer.PlatformId = command.Data.Options.Where(o => string.Equals(o.Name, "id")).First().Value.ToString();

            // Get all messages in channel
            var recruitingChannel = await client.GetChannelAsync(recruitingChannelId) as ISocketMessageChannel;
            var messages = await GetAllChannelMessages(recruitingChannel);

            // Parse messages into teams
            var teams = ParseMessageAsync(messages);

            // No Teams? -> Add Free Agent team
            if(teams.Count() == 0)
            {
                teams.Add(new Team()
                {
                    Name = "Free Agents",
                    Captain = null,
                    Players = new List<Player>()
                });
            }

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
                var freeAgents = teams.Where((t) => t.Name == "Free Agents").First();
                freeAgents.Players.Add(newPlayer);
            }

            foreach(var team in teams)
            {
                // Have we added this team message yet? -> Write team message and move to next team
                if(team.MsgId == 0)
                {
                    await recruitingChannel.SendMessageAsync(team.ToMessage());
                    continue;
                }

                // This is an existing team -> Modify old team message
                await recruitingChannel.ModifyMessageAsync(team.MsgId, (message) => message.Content = team.ToMessage());
            }

            await command.RespondAsync($"Your RL tracker has been added to the recruiting board in channel <#{recruitingChannelId}>", ephemeral: true);
        }

        internal List<Team> ParseMessageAsync(List<IMessage> messages)
        {
            var teams = new List<Team>();

            foreach(var teamMsg in messages)
            {
                teams.Add(Team.ParseTeam(teamMsg.Id, teamMsg.Content));
            }

            return teams;
        }

        internal async Task<List<IMessage>> GetAllChannelMessages(ISocketMessageChannel channel, int limit = 100)
        {
            var msgs = new List<IMessage>();
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            while(messages.Count() > 0 && msgs.Count() < limit)
            {
                msgs.AddRange(messages);
                messages = await channel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();
            }

            return msgs;
        }

        public override SlashCommandProperties Build()
            => new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOption("platform", ApplicationCommandOptionType.String, "Platorm you play on", required: true, choices: new ApplicationCommandOptionChoiceProperties[] { new ApplicationCommandOptionChoiceProperties() { Name = "epic", Value = "epic" }, new ApplicationCommandOptionChoiceProperties() { Name = "steam", Value = "steam" }, new ApplicationCommandOptionChoiceProperties() { Name = "playstation", Value = "psn" }, new ApplicationCommandOptionChoiceProperties() { Name = "xbox", Value = "xbl" }, new ApplicationCommandOptionChoiceProperties() { Name = "tracker", Value = "tracker" } })
                   .AddOption("id", ApplicationCommandOptionType.String, "For steam use your id, others use username, tracker post full tracker", required: true)         
                   .Build();

        private static string UpdateExistingTracker(string userAndTracker, string username, string messageToEdit, string delimeter)
        {
            string[] splitString = messageToEdit.Split("\n");
            var splitStringSet = splitString.ToList();
            int index = splitStringSet.FindIndex(trackerLink => trackerLink.StartsWith($"{username}{delimeter}"));
            splitStringSet.RemoveAt(index);
            splitStringSet.Insert(index, userAndTracker);
            return splitStringSet.Aggregate((res, item) => $"{res}\n{item}").TrimStart();
        }
    }
}
