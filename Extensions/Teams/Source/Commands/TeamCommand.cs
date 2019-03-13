using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Discord.Teams
{
    public class TeamCommand : ModuleBase<TyniBot.CommandContext>
    {
        [Command("teams"), Summary("Output all the possible combinations of teams in random order!")]
        public async Task Teams([Remainder]string message = "")
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
            if (players.Count < 2)
                throw new Exception("Must have at least 2 players.");

            if (players.Count % 2 != 0)
                throw new Exception("Must have an equal number of players.");

            if (players.Count > 6)
                throw new Exception("Only supports up to 6 players total at this time.");

            players = players.Shuffle().ToList();

            var teamSize = players.Count / 2;
            var teams = Combinations.Combine<string>(players, minimumItems: teamSize, maximumItems: teamSize);
            var matches = new List<Tuple<List<string>, List<string>>>();

            while (teams.Count > 0)
            {
                var team1 = teams.First();
                var team2 = teams.Where(l => l.ContainsNone(team1)).First();

                // put teams in tuple
                matches.Add(new Tuple<List<string>, List<string>>(team1, team2));

                teams.Remove(team1);
                teams.Remove(team2);
            }

            return matches;
        }
    }
}
