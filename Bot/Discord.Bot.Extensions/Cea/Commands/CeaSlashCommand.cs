﻿using Discord.Bot;
using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public class CeaSlashCommand : SlashCommand
    {
        public override string Name => "cea";

        public override string Description => "Command to see info from PlayCea.com.";

        public override bool DefaultPermissions => true;

        public override bool IsGlobal => true;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
        };

        private Dictionary<string, ICeaSubCommand> subCommands;

        public CeaSlashCommand() : base()
        {
            List<ICeaSubCommand> subCommands = new()
            {
                new CeaTeamCommand()
            };

            this.subCommands = subCommands.ToDictionary(c => c.OptionBuilder.Name);
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var subCommand = command.Data.Options.Where(o => o.Type.Equals(ApplicationCommandOptionType.SubCommand)).First();
            IReadOnlyDictionary<SlashCommandOptions, string> options = SlashCommandUtils.OptionsToDictionary(subCommand.Options);

            Lazy<List<Team>> lazyTeams = new Lazy<List<Team>>(() => TeamResolver.ResolveTeam(options, command.User));

            if (subCommands.ContainsKey(subCommand.Name))
            {
                await subCommands[subCommand.Name].Run(command, client, options, lazyTeams);
            }
            else
            {
                await command.RespondAsync($"SubCommand {subCommand} not supported", ephemeral: true);
            }            
        }

        public override ApplicationCommandProperties Build()
        {
            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);

            foreach (ICeaSubCommand subCommand in subCommands.Values)
            {
                SlashCommandOptionBuilder optionBuilder = subCommand.OptionBuilder;

                if (subCommand.SupportedOptions.HasFlag(SlashCommandOptions.team))
                {
                    optionBuilder.AddOption(SlashCommandOptions.team.ToString(),
                                ApplicationCommandOptionType.String,
                                "Filter command option to a specific team name.");
                }

                if (subCommand.SupportedOptions.HasFlag(SlashCommandOptions.org))
                {
                    optionBuilder.AddOption(
                        name: SlashCommandOptions.org.ToString(),
                        type: ApplicationCommandOptionType.String,
                        description: "Filter command option to a specific organization (company).");
                }

                

                builder.AddOption(optionBuilder);
            }

            return builder.Build();
        }
    }
}