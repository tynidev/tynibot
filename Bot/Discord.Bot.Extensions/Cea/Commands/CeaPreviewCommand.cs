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
            CeaNextCommand.AddNextMatchToEmbed(builder, team);

            if (!match.Bye)
            {
                CeaRecordCommand.AddRecordStatsToEmbed(builder, match.HomeTeam);
                CeaHistoryCommand.AddHistoryToEmbed(builder, match.HomeTeam);

                CeaRecordCommand.AddRecordStatsToEmbed(builder, match.AwayTeam);
                CeaHistoryCommand.AddHistoryToEmbed(builder, match.AwayTeam);
            }

            return builder.Build();
        }
    }
}
