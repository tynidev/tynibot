using CEA_RL_Bot.DataModel;
using Discord.WebSocket;
using PlayCEA_RL.Configuration;
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
            BracketRound currentRound = currentBrackets.Rounds.Last().First();
            string currentStage = StageMatcher.Lookup(currentRound.RoundName);
            List<StageGroup> stageGroups = ConfigurationManager.Configuration.stageGroups.ToList();
            List<StageGroup> currentStageGroups = stageGroups.Where(g => g.Stage.Equals(currentStage)).ToList();

            List<Embed> embeds = new();
            foreach (StageGroup group in currentStageGroups)
            {
                EmbedBuilder builder = new();
                StringBuilder result = new StringBuilder();
                int page = 0;
                List<Team> teams = group.Teams.OrderBy(t => t.RoundRanking[currentRound]).ToList();
                foreach (Team team in teams)
                {
                    TeamStatistics stats = team.StageStats[currentStage];
                    result.AppendLine($"{team.RoundRanking[currentRound]} {team} [**{stats.MatchWins}**-{stats.MatchLosses}] GoalDiff: {stats.TotalGoalDifferential}");
                    if (result.Length > 800)
                    {
                        builder.AddField(page == 0 ? $"{group.Name} Standings" : $"{group.Name} Continued", result.ToString());
                        result = new StringBuilder();
                        page++;
                    }
                }

                if (result.Length > 0)
                {
                    builder.AddField(page == 0 ? $"{group.Name} Standings" : $"{group.Name} Continued", result.ToString());
                }
                embeds.Add(builder.Build());
            }

            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
        }
    }
}
