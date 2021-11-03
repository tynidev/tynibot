using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaTeamCommand : ICeaSubCommand
    {
        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.TeamsFilteringSupport;

        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "team",
            Description = "Gets information on a team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            StringBuilder sb = new();

            foreach (Team t in lazyTeams.Value)
            {
                sb.AppendLine($"Team: {t.Name}, Current Rank: {t.Rank} [{t.Stats.MatchWins}-{t.Stats.MatchLosses}]");
                sb.AppendLine($"Goal Differential: {t.Stats.TotalGoalDifferential}, Goals/Game: {(double)t.Stats.TotalGoals / t.Stats.TotalGames}");
                foreach (Player p in t.Players)
                {
                    string captainTag = p.Captain ? "(c) " : "";
                    sb.AppendLine($"{captainTag} {p.DiscordId}");
                }
            }

            await command.RespondAsync(sb.ToString());
        }
    }
}
