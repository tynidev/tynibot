using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaTeamCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "team",
            Description = "Gets information on a team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        internal override Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            EmbedBuilder builder = new EmbedBuilder().WithThumbnailUrl(team.ImageURL);
            StringBuilder sb = new();
            sb.AppendLine($"Current Rank: {team.Rank} [{team.Stats.MatchWins}-{team.Stats.MatchLosses}]");
            sb.AppendLine($"Goal Differential: {team.Stats.TotalGoalDifferential}, Goals/Game: {(double)team.Stats.TotalGoals / team.Stats.TotalGames:#.000}");
            builder.AddField(team.Name, sb.ToString());
            AddRosterToEmbed(builder, team);

            return builder.Build();
        }

        internal static void AddRosterToEmbed(EmbedBuilder builder, Team team)
        {
            StringBuilder sb = new();
            foreach (Player p in team.Players)
            {
                string captainTag = p.Captain ? "(c) " : "";
                sb.AppendLine($"{captainTag} {p.DiscordId}");
            }
            builder.AddField($"{team.Name}'s Roster", sb.ToString());
        }
    }
}
