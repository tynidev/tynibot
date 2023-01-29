using Discord.WebSocket;
using PlayCEASharp.Configuration;
using PlayCEASharp.Analysis;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
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
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            BracketSet currentBrackets = LeagueManager.League.Bracket;
            List<BracketRound> currentRounds = currentBrackets.Rounds.Last();
            League league = LeagueManager.League;
            string currentStage = league.StageLookup(currentRounds.First().RoundName);
            List<StageGroup> stageGroups = league.Configuration.stageGroups.ToList();
            List<StageGroup> currentStageGroups = stageGroups.Where(g => g.Stage.Equals(currentStage)).ToList();

            List<Embed> embeds = new();

            if (currentStageGroups.Count == 0)
            {
                await command.RespondAsync("No current stage groups.", ephemeral: ephemeral);
                return;
            }


            Dictionary<Team, BracketRound> currentRoundLookup = new Dictionary<Team, BracketRound>();
            foreach (BracketRound round in currentRounds)
            {
                foreach (Team t in round.Matches.SelectMany(r => r.Teams).ToList())
                {
                    currentRoundLookup[t] = round;
                }
            }

            foreach (StageGroup group in currentStageGroups)
            {
                EmbedBuilder builder = new();
                StringBuilder result = new StringBuilder();
                int page = 0;
                List<Team> teams = group.Teams.Where(t => currentRoundLookup.ContainsKey(t))
                    .OrderBy(t => t.RoundRanking.ContainsKey(currentRoundLookup[t]) ? t.RoundRanking[currentRoundLookup[t]] : 0).ToList();
                foreach (Team team in teams)
                {
                    TeamStatistics stats = team.StageCumulativeRoundStats[currentRoundLookup[team]];
                    string roundRanking = team.RoundRanking.ContainsKey(currentRoundLookup[team]) ? team.RoundRanking[currentRoundLookup[team]].ToString() : "?";
                    result.AppendLine($"**{roundRanking}** {team} [**{stats.MatchWins}**-{stats.MatchLosses}] {team.NameConfiguration.ScoreWord}Diff: {stats.TotalGoalDifferential}");
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

            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
        }
    }
}
