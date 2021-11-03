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

        internal override SlashCommandOptions SupportedOptions => SlashCommandOptions.TeamsFilteringSupport;

        internal override string Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Current Rank: {team.Rank} [{team.Stats.MatchWins}-{team.Stats.MatchLosses}]");
            sb.AppendLine($"Goal Differential: {team.Stats.TotalGoalDifferential}, Goals/Game: {(double)team.Stats.TotalGoals / team.Stats.TotalGames:#.000}");
            foreach (Player p in team.Players)
            {
                string captainTag = p.Captain ? "(c) " : "";
                sb.AppendLine($"{captainTag} {p.DiscordId}");
            }

            return sb.ToString();
        }
    }
}
