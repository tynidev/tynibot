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
            League league = LeagueManager.League;
            if (!league.NextMatchLookup.ContainsKey(team))
            {
                return null;
            }

            EmbedBuilder builder = new();
            MatchResult match = league.NextMatchLookup[team];
            BracketRound round = league.Bracket.Rounds.Last();

            string message = string.Format("{0}'s next match is , {1} ({4}) vs {2} ({5}).{3}",
                team, match.HomeTeam, match.AwayTeam, match.Completed ? $" (Completed) [{match.HomeGamesWon}-{match.AwayGamesWon}]" : "", match.HomeTeam.RoundRanking[round], match.AwayTeam.RoundRanking[round]);

            builder.AddField(team.Name, message);
            
            return builder.Build();
        }
    }
}
