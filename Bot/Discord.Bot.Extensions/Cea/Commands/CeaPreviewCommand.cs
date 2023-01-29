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
    internal class CeaPreviewCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "preview",
            Description = "Gets detailed information on the next match preview for at team or teams.",
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
                if (!league.NextMatchLookup.ContainsKey(team))
                {
                    continue;
                }

                MatchResult match = league.NextMatchLookup[team];

                EmbedBuilder builder = new EmbedBuilder().WithThumbnailUrl(team.ImageURL); ;
                builder.Title = CeaNextCommand.GetNextMatchString(team, league);

                if (!match.Bye)
                {
                    CeaTeamCommand.AddRosterToEmbed(builder, match.HomeTeam);
                    CeaRecordCommand.AddRecordStatsToEmbed(builder, match.HomeTeam);
                    CeaHistoryCommand.AddFullHistoryToEmbed(builder, match.HomeTeam, league);

                    CeaTeamCommand.AddRosterToEmbed(builder, match.AwayTeam);
                    CeaRecordCommand.AddRecordStatsToEmbed(builder, match.AwayTeam);
                    CeaHistoryCommand.AddFullHistoryToEmbed(builder, match.AwayTeam, league);
                }

                embeds.Add(builder.Build());
            }

            return embeds;
        }
    }
}
