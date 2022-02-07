using Discord.WebSocket;
using PlayCEAStats.Analysis;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaRematchesCommand : ICeaSubCommand
    {
        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "rematches",
            Description = "Searches for any rematches in the current stage.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.none;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            Bracket bracket = LeagueManager.League.Bracket.Brackets.First();
            string stage = StageMatcher.Lookup(bracket.Rounds.Last().RoundName);
            string rematches = StageRematchFinder.FindRematches(bracket, stage);

            await command.RespondAsync(rematches, ephemeral: true);
        }
    }
}
