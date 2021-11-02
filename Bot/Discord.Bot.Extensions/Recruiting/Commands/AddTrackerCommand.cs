﻿using Discord;
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
    public class AddTrackerCommand : RecruitingCommand
    {
        public override string Name => "addtracker";

        public override string Description => "Add your RL tracker for recruiting purposes!";

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            //{ 902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) } }, // tynibot test
            //{ 124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(598569589512863764, ApplicationCommandPermissionTarget.Role, true) } }, // msft rl
            //{ 801598108467200031, new List<ApplicationCommandPermission>() } // tyni's server
            { 904804698484260874, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(904867602571100220, ApplicationCommandPermissionTarget.Role, true) } }, // nate server
        };

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

        internal static List<Team> ParseMessageAsync(List<IMessage> messages)
        {
            var teams = new List<Team>();

            foreach(var teamMsg in messages)
            {
                try
                {
                    teams.Add(Team.ParseTeam(teamMsg.Id, teamMsg.Content));
                }
                catch
                {
                    // some other bot command or user typed message is in this channel
                }
            }

            return teams;
        }

        internal static async Task<List<IMessage>> GetAllChannelMessages(ISocketMessageChannel channel, int limit = 100)
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
                   .AddOption("platform", 
                                ApplicationCommandOptionType.String, 
                                "Platorm you play on", 
                                required: true, 
                                choices: 
                                    new ApplicationCommandOptionChoiceProperties[] { new ApplicationCommandOptionChoiceProperties() { Name = "epic", Value = "Epic" }, 
                                        new ApplicationCommandOptionChoiceProperties() { Name = "steam", Value = "Steam" }, 
                                        new ApplicationCommandOptionChoiceProperties() { Name = "playstation", Value = "Playstation" }, 
                                        new ApplicationCommandOptionChoiceProperties() { Name = "xbox", Value = "Xbox" }
                                    })
                   .AddOption("id", ApplicationCommandOptionType.String, "For steam use your id, others use username, tracker post full tracker", required: true)         
                   .Build();
    }
}
