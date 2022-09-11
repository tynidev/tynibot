using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
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
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            BracketSet currentBrackets = LeagueManager.League.Bracket;
            if (currentBrackets == null)
            {
                await command.RespondAsync(text: "No Current Brackets.", ephemeral: ephemeral);
            }

            List<Tuple<string, string, string>> bracketResults = new();
            foreach (Bracket bracket in currentBrackets.Brackets)
            {
                List<BracketRound> rounds = bracket.Rounds;
                int roundIndex = !options.ContainsKey(SlashCommandOptions.week) ? rounds.Count - 1 : int.Parse(options[SlashCommandOptions.week]);
                BracketRound round = rounds[roundIndex];
                StringBuilder sb = new();
                foreach (MatchResult match in round.NonByeMatches)
                {
                    string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                    string awayRank = match.AwayTeam.RoundRanking.ContainsKey(round) ? match.AwayTeam.RoundRanking[round].ToString() : "?";
                    sb.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{homeRank}**){match.HomeTeam} vs (**{awayRank}**){match.AwayTeam}");
                }

                foreach (MatchResult match in round.ByeMatches)
                {
                    string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                    sb.AppendLine($"[BYE] (**{homeRank}**){match.HomeTeam} vs *BYE*");
                }

                bracketResults.Add(new Tuple<string, string, string>(bracket.Name, round.RoundName, sb.ToString()));
            }

            if (bracketResults.All(s => s.Item3.Length < 1024)) 
            {
                List<Embed> embeds = new();
                foreach (Tuple<string, string, string> bracketResult in bracketResults)
                {
                    EmbedBuilder builder = new();
                    builder.Title = bracketResult.Item1;
                    builder.AddField(bracketResult.Item2, bracketResult.Item3);
                    embeds.Add(builder.Build());
                }

                await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
            } else {
                StringBuilder result = new StringBuilder();
                foreach (Tuple<string, string, string> bracketResult in bracketResults)
                {
                    result.AppendLine(bracketResult.Item1);
                    result.AppendLine(bracketResult.Item2);
                    result.AppendLine(bracketResult.Item3);
                }

                await command.RespondAsync(text: result.ToString(), ephemeral: ephemeral);
            }            
        }
    }
}
