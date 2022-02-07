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
            List<List<BracketRound>> rounds = LeagueManager.League.Bracket.Rounds;
            int roundIndex = !options.ContainsKey(SlashCommandOptions.week) ? rounds.Count - 1 : int.Parse(options[SlashCommandOptions.week]);
            List<BracketRound> round = rounds[roundIndex];

            StringBuilder sb = new();

            foreach (BracketRound r in round)
            {
                foreach (MatchResult match in r.NonByeMatches)
                {
                    sb.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{match.HomeTeam.RoundRanking[r]}**){match.HomeTeam} vs (**{match.AwayTeam.RoundRanking[r]}**){match.AwayTeam}");
                }

                foreach (MatchResult match in r.ByeMatches)
                {
                    sb.AppendLine($"[BYE] (**{match.HomeTeam.RoundRanking[r]}**){match.HomeTeam} vs *BYE*");
                }
            }
            
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");

            if (sb.ToString().Length < 1024) 
            {
                EmbedBuilder builder = new();
                builder.AddField(round.First().RoundName, sb.ToString());
                await command.RespondAsync(embed: builder.Build(), ephemeral: ephemeral);
            } else {
                string text = $"{round.First().RoundName}\n{sb}";
                await command.RespondAsync(text: text, ephemeral: ephemeral);
            }            
        }
    }
}
