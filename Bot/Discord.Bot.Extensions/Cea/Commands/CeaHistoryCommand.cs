using Discord.WebSocket;
using PlayCEAStats.Analysis;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaHistoryCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "history",
            Description = "Gets all match history for a team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        internal override Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            EmbedBuilder builder = new();
            AddFullHistoryToEmbed(builder, team);
            return builder.Build();
        }

        internal static void AddFullHistoryToEmbed(EmbedBuilder builder, Team team)
        {
            League league = LeagueManager.League;
            foreach (BracketSet bracket in league.Brackets)
            {
                AddHistoryToEmbed(builder, team, bracket);
            }
        }

        internal static void AddHistoryToEmbed(EmbedBuilder builder, Team team, BracketSet bracket)
        {
            StringBuilder sb = new();
            foreach (BracketRound round in bracket.Rounds.SelectMany(r => r))
            {
                foreach (MatchResult result in round.Matches)
                {
                    if (result.HomeTeam == team || result.AwayTeam == team)
                    {
                        string homeRank = result.HomeTeam.RoundRanking.ContainsKey(round) ? result.HomeTeam.RoundRanking[round].ToString() : "?";
                        string awayRank = result.AwayTeam.RoundRanking.ContainsKey(round) ? result.AwayTeam.RoundRanking[round].ToString() : "?";
                        string awayString = result.Bye ? "BYE" : $"{result.AwayTeam}[{awayRank}]";
                        sb.AppendLine($"[{result.HomeGamesWon}-{result.AwayGamesWon}] {result.HomeTeam} [{homeRank}] vs {awayString}");
                    }
                }
            }

            builder.AddField($"{team.Name}'s {StageMatcher.Lookup(bracket.Rounds.First().First().RoundName)} Match History", sb.ToString());
        }
    }
}
