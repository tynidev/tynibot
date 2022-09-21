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
    internal class CeaNextCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "next",
            Description = "Gets information on the next matchup for at team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        internal override Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            EmbedBuilder builder = new EmbedBuilder().WithThumbnailUrl(team.ImageURL);
            if (!AddNextMatchToEmbed(builder, team))
            {
                return null;
            }
            
            return builder.Build();
        }

        internal static bool AddNextMatchToEmbed(EmbedBuilder builder, Team team)
        {
            League league = LeagueManager.League;

            if (!league.NextMatchLookup.ContainsKey(team))
            {
                return false;
            }

            MatchResult match = league.NextMatchLookup[team];
            List<BracketRound> rounds = league.Bracket.Rounds.Last();
            BracketRound round = rounds.Where(r => r.Matches.SelectMany(m => new List<Team>() { m.HomeTeam, m.AwayTeam }).Contains(team)).First();

            string message;

            if (match.Bye)
            {
                message = string.Format("{0}'s next match is a *BYE*.",
                    team);
            }
            else
            {
                string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                string awayRank = match.AwayTeam.RoundRanking.ContainsKey(round) ? match.AwayTeam.RoundRanking[round].ToString() : "?";
                message = string.Format("{0}'s next match is {1} ({4}) vs {2} ({5}).{3}",
                    team, match.HomeTeam, match.AwayTeam, match.Completed ? $" (Completed) [{match.HomeGamesWon}-{match.AwayGamesWon}]" : "", homeRank, awayRank);
            }

            builder.AddField(team.Name, message);
            return true;
        }

        internal static string GetNextMatchString(Team team)
        {
            League league = LeagueManager.League;

            if (!league.NextMatchLookup.ContainsKey(team))
            {
                return "No match found.";
            }

            MatchResult match = league.NextMatchLookup[team];
            List<BracketRound> rounds = league.Bracket.Rounds.Last();
            BracketRound round = rounds.Where(r => r.Matches.SelectMany(m => new List<Team>() { m.HomeTeam, m.AwayTeam }).Contains(team)).First();
            if (match.Bye)
            {
                return string.Format("{0}'s next match is a *BYE*",
                    team);
            } 
            else
            {
                string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                string awayRank = match.AwayTeam.RoundRanking.ContainsKey(round) ? match.AwayTeam.RoundRanking[round].ToString() : "?";
                return string.Format("{0} ({3}) vs {1} ({4}) {2}",
                    match.HomeTeam, match.AwayTeam, match.Completed ? $" (Completed) [{match.HomeGamesWon}-{match.AwayGamesWon}]" : "", homeRank, awayRank);
            }
        }
    }
}
