using Discord.Bot;
using Discord.Bot.Utils;
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

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => GuildIdMappings.defaultSlashCommandPermissions;

        private readonly Dictionary<string, ICeaSubCommand> subCommands;

        public CeaSlashCommand() : base()
        {
            List<ICeaSubCommand> subCommands = new()
            {
                new CeaTeamCommand(),
                new CeaNextCommand(),
                new CeaRecordCommand(),
                new CeaHistoryCommand(),
                new CeaRoundCommand(),
                new CeaPreviewCommand(),
                new CeaStandingsCommand(),
                new CeaForceRefreshCommand(),
            };

            this.subCommands = subCommands.ToDictionary(c => c.OptionBuilder.Name);
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient, Guild guild)
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

                SlashCommandUtils.AddCommonOptionProperties(optionBuilder, subCommand.SupportedOptions);

                builder.AddOption(optionBuilder);
            }

            return builder.Build();
        }
    }
}
