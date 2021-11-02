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
    public class MoveTrackedUserCommand : RecruitingCommand
    {
        public override string Name => "movetrackeduser";

        public override string Description => "Move a tracked user to a team or off the recruiting board. Add team option to move to a team";


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

            if (!recruitingChannelForGuild.TryGetValue(channel.Guild.Id, out var recruitingChannelId))
            {
                await command.RespondAsync("Channel is not part of a guild that supports recruiting", ephemeral: true);
                return;
            }

            // Get all messages in channel
            var recruitingChannel = await client.GetChannelAsync(recruitingChannelId) as ISocketMessageChannel;
            var messages = await AddTrackerCommand.GetAllChannelMessages(recruitingChannel);

            // Parse messages into teams
            var teams = AddTrackerCommand.ParseMessageAsync(messages);

            var options = command.Data.Options.ToDictionary(o => o.Name, o => o);
            var discordUser = options["username"].Value.ToString();

            // Player not exist? -> respond with error
            (var oldTeam, var player) = Team.FindPlayer(teams, discordUser);
            if (player == null)
            {
                await command.RespondAsync($"User {discordUser} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            // If player was captain of old team remove that teams captain
            if (oldTeam.Captain?.DiscordUser == player.DiscordUser)
                oldTeam.Captain = null;

            // Move Player
            oldTeam.Players.Remove(player);
            // Update old team message
            await recruitingChannel.ModifyMessageAsync(oldTeam.MsgId, (message) => message.Content = oldTeam.ToMessage());

            // Team option specified? -> move player
            if (options.ContainsKey("team"))
            {
                var teamName = options["team"].Value.ToString();

                // Team not exist? -> respond with error
                var newTeam = Team.FindTeam(teams, teamName);
                if (newTeam == null)
                {
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
                    await recruitingChannel.SendMessageAsync(newTeam.ToMessage());
                }
                else
                {
                    await recruitingChannel.ModifyMessageAsync(newTeam.MsgId, (message) => message.Content = newTeam.ToMessage());
                }

                await command.RespondAsync($"You have moved user {discordUser} from {oldTeam.Name} -> {newTeam.Name}", ephemeral: true);
            }
            else
            {
                await command.RespondAsync($"You have removed user {discordUser} from {oldTeam.Name}", ephemeral: true);
            }
        }

        public override SlashCommandProperties Build()
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
