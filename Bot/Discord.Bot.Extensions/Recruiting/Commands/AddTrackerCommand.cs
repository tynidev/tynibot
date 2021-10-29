using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TyniBot.Models;
using System.Collections.Immutable;

namespace TyniBot.Commands
{
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
            this.GuildIdsAndPermissions.Add(124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(598569589512863764, ApplicationCommandPermissionTarget.Role, true) }); // msft rl
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command)
        {
            await command.RespondAsync("addtracker command has beemn used");
        }

        public override SlashCommandProperties CreateSlashCommand()
            => new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOption("EpicId", ApplicationCommandOptionType.String, "Your Epic ID to retrieve RL tracker", required: true)         
                   .Build();
    }
}
