using Discord.WebSocket;
using PlayCEAStats.Analysis;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaStandingsCommand : ICeaSubCommand
    {
        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "standings",
            Description = "Displays current standings with details.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.post;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            BracketSet currentBrackets = LeagueManager.League.Bracket;
            List<Team> teams = currentBrackets.Teams;
            BracketRound currentRound = currentBrackets.Rounds.First().First();
            string currentStage = StageMatcher.Lookup(currentRound.RoundName);
            teams = teams.OrderBy(t => t.RoundRanking[currentRound]).ToList();

            List<Embed> embeds = new();
            EmbedBuilder builder = new();
            StringBuilder result = new StringBuilder();
            int page = 0;
            foreach (Team team in teams)
            {
                TeamStatistics stats = team.StageStats[currentStage];
                result.AppendLine($"{team.RoundRanking[currentRound]} {team} [**{stats.MatchWins}**-{stats.MatchLosses}] GoalDiff: {stats.TotalGoalDifferential}");
                if (result.Length > 800)
                {
                    builder.AddField(page == 0 ? "Current Standings" : "Standings Continued", result.ToString());
                    result = new StringBuilder();
                    page++;
                }
            }

            builder.AddField(page == 0 ? "Current Standings" : "Standings Continued", result.ToString());
            embeds.Add(builder.Build());
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
        }
    }
}
