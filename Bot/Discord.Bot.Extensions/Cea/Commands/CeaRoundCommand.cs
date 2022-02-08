﻿using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
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

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.post | SlashCommandOptions.week;

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            BracketSet currentBrackets = LeagueManager.League.Bracket;
            List<Tuple<string, string, string>> bracketResults = new();

            foreach (Bracket bracket in currentBrackets.Brackets)
            {
                List<BracketRound> rounds = bracket.Rounds;
                int roundIndex = !options.ContainsKey(SlashCommandOptions.week) ? rounds.Count - 1 : int.Parse(options[SlashCommandOptions.week]);
                BracketRound round = rounds[roundIndex];
                StringBuilder sb = new();
                foreach (MatchResult match in round.NonByeMatches)
                {
                    sb.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{match.HomeTeam.RoundRanking[round]}**){match.HomeTeam} vs (**{match.AwayTeam.RoundRanking[round]}**){match.AwayTeam}");
                }

                foreach (MatchResult match in round.ByeMatches)
                {
                    sb.AppendLine($"[BYE] (**{match.HomeTeam.RoundRanking[round]}**){match.HomeTeam} vs *BYE*");
                }

                bracketResults.Add(new Tuple<string, string, string>(bracket.Name, round.RoundName, sb.ToString()));
            }
            
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");

            if (bracketResults.All(s => s.Item3.Length < 1024)) 
            {
                List<Embed> embeds = new();
                foreach (Tuple<string, string, string> bracketResult in bracketResults)
                {
                    EmbedBuilder builder = new();
                    builder.Title = bracketResult.Item1;
                    builder.AddField(bracketResult.Item2, bracketResult.Item3);
                    embeds.Add(builder.Build());
                }

                await command.RespondAsync(embeds: embeds.ToArray(), ephemeral: ephemeral);
            } else {
                StringBuilder result = new StringBuilder();
                foreach (Tuple<string, string, string> bracketResult in bracketResults)
                {
                    result.AppendLine(bracketResult.Item1);
                    result.AppendLine(bracketResult.Item2);
                    result.AppendLine(bracketResult.Item3);
                }

                await command.RespondAsync(text: result.ToString(), ephemeral: ephemeral);
            }            
        }
    }
}
