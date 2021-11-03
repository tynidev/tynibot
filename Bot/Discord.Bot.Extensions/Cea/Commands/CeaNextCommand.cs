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
            EmbedBuilder builder = new();
            MatchResult match = LeagueManager.League.NextMatchLookup[team];

            string message = string.Format("{0}'s next match is in week {1}, {2} vs {3}.{4}",
                team, match.Round + 1, match.HomeTeam, match.AwayTeam, match.Completed ? " (Completed)" : "");

            builder.AddField(team.Name, message);
            
            return builder.Build();
        }
    }
}
