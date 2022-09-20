using Discord.WebSocket;
using PlayCEASharp.Analysis;
using PlayCEASharp.Configuration;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaRoundCommand : ICeaSubCommand
    {
        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "round",
            Description = "Displays all matchups for the current round.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.post;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");
            BracketSet currentBrackets = LeagueManager.League.Bracket;
            if (currentBrackets == null)
            {
                await command.RespondAsync(text: "No Current Brackets.", ephemeral: ephemeral);
            }

            List<BracketRound> currentRounds = currentBrackets.Rounds.Last();
            string currentStage = StageMatcher.Lookup(currentRounds.First().RoundName);
            List<StageGroup> stageGroups = ConfigurationManager.Configuration.stageGroups.ToList();
            List<StageGroup> currentStageGroups = stageGroups.Where(g => g.Stage.Equals(currentStage)).ToList();

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

            List<Embed> embeds = new();
            foreach (StageGroup group in currentStageGroups)
            {
                EmbedBuilder builder = new();
                StringBuilder result = new StringBuilder();
                int page = 0;
                BracketRound round = currentRoundLookup[group.Teams.First()];
                foreach (MatchResult match in round.NonByeMatches)
                {
                    string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                    string awayRank = match.AwayTeam.RoundRanking.ContainsKey(round) ? match.AwayTeam.RoundRanking[round].ToString() : "?";
                    result.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{homeRank}**){match.HomeTeam} vs (**{awayRank}**){match.AwayTeam}");
                    if (result.Length > 800)
                    {
                        builder.AddField(page == 0 ? $"{group.Name}" : $"{group.Name} Continued", result.ToString());
                        result = new StringBuilder();
                        page++;
                    }
                }

                foreach (MatchResult match in round.ByeMatches)
                {
                    string homeRank = match.HomeTeam.RoundRanking.ContainsKey(round) ? match.HomeTeam.RoundRanking[round].ToString() : "?";
                    result.AppendLine($"[BYE] (**{homeRank}**){match.HomeTeam} vs *BYE*");
                    if (result.Length > 800)
                    {
                        builder.AddField(page == 0 ? $"{group.Name}" : $"{group.Name} Continued", result.ToString());
                        result = new StringBuilder();
                        page++;
                    }
                }

                if (result.Length > 0)
                {
                    builder.AddField(page == 0 ? $"{group.Name}" : $"{group.Name} Continued", result.ToString());
                }
                embeds.Add(builder.Build());
            }

            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
        }
    }
}
