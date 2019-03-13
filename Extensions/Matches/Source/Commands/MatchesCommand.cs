using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Discord.Matches
{
    public class MatchesCommand : ModuleBase<TyniBot.CommandContext>
    {
        [Command("matches"), Summary("Output all the possible combinations of teams in random order!")]
        public async Task Matches([Remainder]string message = "")
        {
            var users = Context.Message.MentionedUsers.Select(s => s.Mention).Shuffle().ToList();

            try
            {
                var matches = GetUniqueMatches(users);
                await OutputUniqueMatches(matches, Context.Channel);
            }
            catch(Exception e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
            }
        }

        public static async Task<IMessage> OutputUniqueMatches(List<Tuple<List<string>, List<string>>> matches, IMessageChannel channel)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var team1 = string.Join(' ', match.Item1);
                var team2 = string.Join(' ', match.Item2);

                string team1Str = $"Orange: {team1}";
                string team2Str = $"Blue: {team2}";

                embedBuilder.AddField($"Match {i + 1}:", team1Str + "\r\n" + team2Str);
            }

            return await channel.SendMessageAsync($"**Unique Matches: {matches.Count}**", false, embedBuilder.Build());
        }

        public static List<Tuple<List<string>, List<string>>> GetUniqueMatches(List<string> players)
        {
            players = players.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList(); // remove empty entries

            if (players.Count < 2)
                throw new Exception("Must have at least 2 unique players.");

            if (players.Count > 6)
                throw new Exception("Only supports up to 6 unique players total at this time.");

            if (players.Count % 2 != 0)
                throw new Exception("Must have an equal number of unique players.");

            players = players.Shuffle().ToList(); // randmoize initial ordering

            var teamSize = players.Count / 2;
            var uniqueTeams = Combinations.Combine<string>(players, minimumItems: teamSize, maximumItems: teamSize);
            var matches = new List<Tuple<List<string>, List<string>>>();

            while (uniqueTeams.Count > 0)
            {
                var team1 = uniqueTeams.First();
                var team2 = uniqueTeams.Where(l => l.ContainsNone(team1)).First();

                matches.Add(new Tuple<List<string>, List<string>>(team1, team2));

                uniqueTeams.Remove(team1);
                uniqueTeams.Remove(team2);
            }

            return matches;
        }
    }
}
