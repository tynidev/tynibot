using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaRoundCommand : ICeaSubCommand
    {
        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "round",
            Description = "Displays all matchups for the current round.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.post | SlashCommandOptions.week;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            List<BracketRound> rounds = LeagueManager.League.Bracket.Rounds;
            int roundIndex = !options.ContainsKey(SlashCommandOptions.week) ? rounds.Count - 1 : int.Parse(options[SlashCommandOptions.week]);
            BracketRound r = rounds[roundIndex];

            StringBuilder sb = new();
            foreach (MatchResult match in r.Matches)
            {
                sb.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{match.HomeTeam.RoundRanking[r]}**){match.HomeTeam} vs (**{match.AwayTeam.RoundRanking[r]}**){match.AwayTeam}");
            }

            EmbedBuilder builder = new();
            builder.AddField(r.RoundName, sb.ToString());

            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            await command.RespondAsync(embed: builder.Build(), ephemeral: ephemeral);
        }
    }
}
