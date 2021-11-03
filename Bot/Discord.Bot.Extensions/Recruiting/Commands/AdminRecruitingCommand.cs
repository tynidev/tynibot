﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Commands
{
    class AdminRecruitingCommand : RecruitingCommand
    {
        public override string Name => "recruiting-admin";

        public override string Description => "Manage teams for CEA recruiting.";

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            { 902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) } }, // tynibot test
            { 124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(469941381075435523, ApplicationCommandPermissionTarget.Role, true), new ApplicationCommandPermission(480419333995233280, ApplicationCommandPermissionTarget.Role, true) } }, // msft rl
            { 801598108467200031, new List<ApplicationCommandPermission>() }, // tyni's server
            { 904804698484260874, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(904867602571100220, ApplicationCommandPermissionTarget.Role, true) } }, // nate server
        };

        public override SlashCommandProperties Build()
        {           
            var moveCmd = new SlashCommandOptionBuilder()
            {
                Name = "move",
                Description = "Move a tracked user to a team.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            moveCmd.AddOption("username", ApplicationCommandOptionType.User, "Username of user to move", required: true);
            moveCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to move user to", required: true);
            moveCmd.AddOption("captain", ApplicationCommandOptionType.Boolean, "Is this user the captain of the team?", required: false);

            var removeCmd = new SlashCommandOptionBuilder()
            {
                Name = "remove",
                Description = "Remove a tracked user.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            removeCmd.AddOption("username", ApplicationCommandOptionType.User, "Username of user to remove", required: true);

            var deleteTeamCmd = new SlashCommandOptionBuilder()
            {
                Name = "deleteteam",
                Description = "Remove team.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            deleteTeamCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to remove", required: true);

            var builder = new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOptions(moveCmd, removeCmd, deleteTeamCmd);

            return builder.Build();
        }
    }
}
