using Discord.WebSocket;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaForceRefreshCommand : ICeaSubCommand
    {
        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "forcerefresh",
            Description = "Forces a refresh from the playcea website.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.none;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            // Responding prior to update as update may take longer than 3s which is the maximum timeout for discord command.
            await command.RespondAsync("Refresh Started.", ephemeral: true);
            LeagueManager.ForceUpdate();
        }
    }
}
