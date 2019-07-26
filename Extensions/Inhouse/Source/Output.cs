using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot;
using EmojiData;
using Discord.WebSocket;

namespace Discord.Inhouse
{
    public class Output
    {
        public static async Task<IUserMessage> HelpText(IMessageChannel channel)
        {
            var commands = typeof(InhouseCommand).GetMethods()
                      .Where(m => m.GetCustomAttributes(typeof(SummaryAttribute), false).Length > 0)
                      .ToArray();

            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (var command in commands)
            {
                var name = (CommandAttribute)command.GetCustomAttributes(typeof(CommandAttribute), false)[0];
                var summary = (SummaryAttribute)command.GetCustomAttributes(typeof(SummaryAttribute), false)[0];

                if (!string.IsNullOrWhiteSpace(name.Text))
                {
                    // Get the command Summary attribute information
                    string embedFieldText = summary.Text ?? "No description available\n";

                    embedBuilder.AddField(name.Text, embedFieldText);
                }
            }

            return await channel.SendMessageAsync("**Inhouse Commands:** ", false, embedBuilder.Build());
        }

        /*
         Match Output
         @param matches - List of Match Pairings
         @param channel - Channel Object for the Output Channel
        */
        public static async Task<IMessage> OutputUniqueMatches(List<Tuple<List<Player>, List<Player>>> matches, IMessageChannel channel)
        {

            int MaxMatchDisplayCount = 5;
            EmbedBuilder embedBuilder = new EmbedBuilder();

            for (int i = 0; i < matches.Count && i < MaxMatchDisplayCount; i++)
            {
                var match = matches[i];
                var team1 = string.Join('\n', match.Item1.Select(m => m.Username));
                var team2 = string.Join('\n', match.Item2.Select(m => m.Username));

                int team1MMR = match.Item1.Sum(item => item.MMR) / match.Item1.Count;
                int team2MMR = match.Item2.Sum(item => item.MMR) / match.Item2.Count;

                string team1Str = $"Orange: {team1}";
                string team2Str = $"Blue: {team2}";

                embedBuilder.AddField($"Match {i + 1}", "\n");
                embedBuilder.AddField($"Orange ({team1MMR}):", team1Str, true);
                embedBuilder.AddField($"Blue ({team2MMR}):", team2Str, true);
            }

            return await channel.SendMessageAsync($"**Unique Matches: {matches.Count}**", false, embedBuilder.Build());
        }

        public static async Task<IUserMessage> QueueStarted(IMessageChannel channel, InhouseQueue queue)
        {
            return await channel.SendMessageAsync($"New Inhouse Queue created named {queue.Name}!");
        }

        public static async Task<IUserMessage> PlayersAdded(IMessageChannel channel, InhouseQueue queue, List<Player> list)
        {
            string players = string.Join("\r\n", queue.Players.Select(p => p.Value.Username));
            return await channel.SendMessageAsync($"Players were added succesfully!\r\n\r\n Queue: {players}");
        }

        public static async Task<IUserMessage> PlayersRemoved(IMessageChannel channel, InhouseQueue queue, List<Player> list)
        {
            string players = string.Join("\r\n", queue.Players.Select(p => p.Value.Username));
            return await channel.SendMessageAsync($"Players were removed succesfully!\r\n\r\n Queue: {players}");
        }
    }
}
