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
                // Get the command Summary attribute information
                string embedFieldText = summary.Text ?? "No description available\n";

                embedBuilder.AddField(name.Text, embedFieldText);
            }

            return await channel.SendMessageAsync("**Inhouse Commands:** ", false, embedBuilder.Build());
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
