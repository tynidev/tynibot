using Discord.WebSocket;
using PlayCEASharp.Analysis;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
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

        internal override List<Embed> Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            return GetEmbed(team);
        }

        internal static List<Embed> GetEmbed(Team team)
        {
            List<Embed> embeds = new List<Embed>();
            List<League> leagues = LeagueManager.LeagueLookup[team];
            foreach (League league in leagues)
            {
                EmbedBuilder builder = new EmbedBuilder().WithThumbnailUrl(team.ImageURL);
                AddFullHistoryToEmbed(builder, team, league);
                embeds.Add(builder.Build());
            }

            return embeds;
        }

        internal static void AddFullHistoryToEmbed(EmbedBuilder builder, Team team, League league)
        {
            foreach (BracketSet bracket in league.Brackets)
            {
                AddHistoryToEmbed(builder, team, bracket, league);
            }
        }

        internal static void AddHistoryToEmbed(EmbedBuilder builder, Team team, BracketSet bracket, League league)
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

            builder.AddField($"{team.Name}'s {league.StageLookup(bracket.Rounds.First().First().RoundName)} Match History", sb.ToString());
        }
    }
}
