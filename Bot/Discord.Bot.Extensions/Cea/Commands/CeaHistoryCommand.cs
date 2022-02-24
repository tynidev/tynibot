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
            AddHistoryToEmbed(builder, team);
            return builder.Build();
        }

        internal static void AddHistoryToEmbed(EmbedBuilder builder, Team team)
        {
            StringBuilder sb = new();
            League league = LeagueManager.League;
            foreach (BracketRound round in league.Bracket.Rounds.SelectMany(r => r))
            {
                foreach (MatchResult result in round.Matches)
                {
                    if (result.HomeTeam == team || result.AwayTeam == team)
                    {
                        sb.AppendLine($"[{result.HomeGamesWon}-{result.AwayGamesWon}] {result.HomeTeam} vs {result.AwayTeam}");
                    }
                }
            }

            builder.AddField($"{team.Name}'s Match History", sb.ToString());
        }
    }
}
