﻿using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;
using Discord.Bot;
using System;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    public class MoveTrackedUserCommand : SlashCommand
    {
        public override string Name => "movetrackeduser";

        public override string Description => "Move a tracked user to a team or off the recruiting board. Do not add a team option to remove from the recruiting board";

        public override bool DefaultPermissions => false;

        public override bool IsGlobal => false;

        private static readonly ImmutableDictionary<ulong, ulong> recruitingChannelForGuild = new Dictionary<ulong, ulong> {
            { 902581441727197195, 903521423522398278}, //tynibot test
            { 598569589512863764,  541894310258278400} //msft rl
        }.ToImmutableDictionary();

        public MoveTrackedUserCommand()
            : base()
        {
            this.GuildIdsAndPermissions.Add(902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) }); // tynibot test
            this.GuildIdsAndPermissions.Add(124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(598569589512863764, ApplicationCommandPermissionTarget.Role, true) }); // msft rl
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var channel = command.Channel as SocketGuildChannel;

            if (!recruitingChannelForGuild.TryGetValue(channel.Guild.Id, out var recruitingChannelId))
            {
                await command.RespondAsync("Channel is not part of a guild that supports recruiting", ephemeral: true);
                return;
            }

            IMessage messageToEdit = null;
            int count = 0;
            var recruitingChannel = await client.GetChannelAsync(recruitingChannelId) as ISocketMessageChannel;

            var messages = await recruitingChannel.GetMessagesAsync().FlattenAsync();
            while (messageToEdit == null && count < 10 && messages.Count() > 0)
            {
                messageToEdit = messages.Where(m => m.Author.Id == client.CurrentUser.Id).FirstOrDefault();

                if (messageToEdit != null)
                {
                    break;
                }
                messages = await recruitingChannel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();
                count++;
            }

            if (messageToEdit == null)
            {
                messageToEdit = await recruitingChannel.SendMessageAsync("__Free Agents__");
            }

            var newContent = messageToEdit.Content;
            var options = command.Data.Options.ToDictionary(o => o.Name, o => o);
            var nameToUse = options["username"].Value;
            if (messageToEdit.Content.Contains($"\n{nameToUse}:"))
            {
                string[] splitString = messageToEdit.Content.Split("\n");
                var splitStringSet = splitString.ToList();
                int index = splitStringSet.FindIndex(trackerLink => trackerLink.StartsWith($"{nameToUse}:"));
                string trackerString = splitStringSet[index];
                splitStringSet.RemoveAt(index);

                if (options.ContainsKey("team"))
                {
                    int teamIndex = splitStringSet.FindIndex(team => team.Contains(options["team"].Value.ToString(), StringComparison.OrdinalIgnoreCase));

                    if (teamIndex == -1)
                    {
                        teamIndex = splitStringSet.FindIndex(team => team.Contains("Free Agents", StringComparison.OrdinalIgnoreCase));

                        splitStringSet.Insert(teamIndex, $"__{options["team"].Value}__");
                    }

                    if (options.ContainsKey("captain") && (bool)options["captain"].Value)
                    {
                        trackerString.Insert(trackerString.IndexOf(":"), "| Captain");
                    }

                    splitStringSet.Insert(teamIndex + 1, trackerString);
                }

                newContent = splitStringSet.Aggregate((res, item) => $"{res}\n{item}").TrimStart();
                await recruitingChannel.SendMessageAsync(newContent);

                await messageToEdit.DeleteAsync();
                await command.RespondAsync($"You have {(options.ContainsKey("team") ? "moved" : "removed")} user {nameToUse}", ephemeral: true);
            }
            else
            {
                await command.RespondAsync($"User {nameToUse} does not exist in the recruiting table");
            }
        }

        public override SlashCommandProperties CreateSlashCommand()
            => new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOption("username", ApplicationCommandOptionType.String, "Username of user to move", required: true)
                   .AddOption("team", ApplicationCommandOptionType.String, "Team to move user to. Do not include option if the user is being removed", required: false)
                   .AddOption("captain", ApplicationCommandOptionType.Boolean, "Is this user the captain of the team?", required: false)
                   .Build();
    }
}
