﻿using Discord;
using Discord.Bot.Utils;
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

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => GuildIdMappings.adminRecruitingPermissions;

        public override SlashCommandProperties Build()
        {
            // TODO: Add directly to a team
            var addCmd = new SlashCommandOptionBuilder()
            {
                Name = "adminadd",
                Description = "Add a user to the recruiting board",
                Type = ApplicationCommandOptionType.SubCommand
            };

            addCmd.AddOption("username", ApplicationCommandOptionType.User, "Username of user to move", isRequired: true); 
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
            addCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to add this user to. If left blank, defaults to Free Agents", isRequired: false);

            var moveCmd = new SlashCommandOptionBuilder()
            {
                Name = "move",
                Description = "Move a tracked user to a team.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            moveCmd.AddOption("username", ApplicationCommandOptionType.User, "Username of user to move", isRequired: true);
            moveCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to move user to", isRequired: true);
            moveCmd.AddOption("captain", ApplicationCommandOptionType.Boolean, "Is this user the captain of the team?", isRequired: false);

            var removeCmd = new SlashCommandOptionBuilder()
            {
                Name = "remove",
                Description = "Remove a tracked user.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            removeCmd.AddOption("username", ApplicationCommandOptionType.User, "Username of user to remove", isRequired: true);

            var deleteTeamCmd = new SlashCommandOptionBuilder()
            {
                Name = "deleteteam",
                Description = "Remove team.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            deleteTeamCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to remove", isRequired: true);

            var lookingForPlayersCmd = new SlashCommandOptionBuilder()
            {
                Name = "lookingforplayers",
                Description = "Mark your team as looking for players or not.",
                Type = ApplicationCommandOptionType.SubCommand
            };
            lookingForPlayersCmd.AddOption("team", ApplicationCommandOptionType.String, "Team to mark as looking for players", isRequired: true);
            lookingForPlayersCmd.AddOption("looking", ApplicationCommandOptionType.Boolean, "Are you looking for new players", isRequired: true);

            var builder = new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .AddOptions(addCmd, moveCmd, removeCmd, deleteTeamCmd, lookingForPlayersCmd);

            return builder.Build();
        }
    }
}
