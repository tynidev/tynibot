using Discord;
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
    public class AddTrackerCommand : SlashCommand
    {
        public override string Name => "addtracker";

        public override string Description => "Add your RL tracker for recruiting purposes!";

        public override bool DefaultPermissions => false;

        public override bool IsGlobal => false;

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
            var user = command.User as SocketGuildUser;
            var nameToUse = user.Nickname ?? user.Username;
            string trackerUri = string.Equals(command.Data.Options.Where(o => string.Equals(o.Name, "platform")).First().Value.ToString(), "tracker", StringComparison.OrdinalIgnoreCase) ? command.Data.Options.Where(o => string.Equals(o.Name, "id")).First().Value.ToString() : $"https://rocketleague.tracker.network/rocket-league/profile/{command.Data.Options.Where(o => string.Equals(o.Name, "platform")).First().Value}/{Uri.EscapeUriString(command.Data.Options.Where(o => string.Equals(o.Name, "id")).First().Value.ToString())}/overview";
            string userAndTracker = $"{nameToUse}: {trackerUri}";

            if (messageToEdit.Content.Contains($"\n{nameToUse}:"))
            {
                newContent = UpdateExistingTracker(userAndTracker, nameToUse, messageToEdit.Content, ":");
            }
            else if (messageToEdit.Content.Contains($"{nameToUse} |"))
            {
                userAndTracker = $"{nameToUse} | Captain: {trackerUri}";
                newContent = UpdateExistingTracker(userAndTracker, nameToUse, messageToEdit.Content, " |");                
            }
            else
            {
                newContent = $"{newContent}\n{userAndTracker}";
            }

            await recruitingChannel.SendMessageAsync(newContent);
            await messageToEdit.DeleteAsync();
            await command.RespondAsync($"Your RL tracker has been added to the recruiting board in channel #{(client.GetChannel(recruitingChannelId) as SocketGuildChannel).Name}", ephemeral: true);
        }

        public override SlashCommandProperties CreateSlashCommand()
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
