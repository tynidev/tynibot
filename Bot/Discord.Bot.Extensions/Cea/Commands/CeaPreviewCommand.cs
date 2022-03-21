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
    internal class CeaPreviewCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "preview",
            Description = "Gets detailed information on the next match preview for at team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        internal override Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            League league = LeagueManager.League;
            if (!league.NextMatchLookup.ContainsKey(team))
            {
                return null;
            }

            MatchResult match = league.NextMatchLookup[team];

            EmbedBuilder builder = new();
            builder.Title = CeaNextCommand.GetNextMatchString(team);

            if (!match.Bye)
            {
                CeaTeamCommand.AddRosterToEmbed(builder, match.HomeTeam);
                CeaRecordCommand.AddRecordStatsToEmbed(builder, match.HomeTeam);
                CeaHistoryCommand.AddFullHistoryToEmbed(builder, match.HomeTeam);

                CeaTeamCommand.AddRosterToEmbed(builder, match.AwayTeam);
                CeaRecordCommand.AddRecordStatsToEmbed(builder, match.AwayTeam);
                CeaHistoryCommand.AddFullHistoryToEmbed(builder, match.AwayTeam);
            }

            return builder.Build();
        }
    }
}
