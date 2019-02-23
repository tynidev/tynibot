using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Recruiting : IChannelHandler
    {
        public async Task Execute(CommandContext context, ServiceProvider serviceProvider)
        {
            var message = context.Message;
            var channel = context.Channel;

            KeyValuePair<string, string> addition = ParseMsg(message);
            await message.DeleteAsync();

            var boardMsg = (await channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();

            Dictionary<string, string> board = ParseContent(boardMsg.Content);

            if (board.ContainsKey(addition.Key))
            {
                board[addition.Key] = addition.Value;
            }
            else
            {
                board.Add(addition.Key, addition.Value);
            }

            await boardMsg.DeleteAsync();

            await channel.SendMessageAsync(WriteBoard(board));
        }

        private string WriteBoard(Dictionary<string, string> board)
        {
            var pairs = board.Select(x => $"{x.Key} | {x.Value}");
            return string.Join("\r\n", pairs);
        }

        private KeyValuePair<string, string> ParseMsg(IUserMessage message)
        {
            Uri uri = new Uri(message.Content);
            return new KeyValuePair<string, string>(message.Author.Username, uri.AbsoluteUri);
        }

        private Dictionary<string, string> ParseContent(string content)
        {
            var lines = Regex.Split(content, "\r\n|\r|\n");
            return lines.Select(x =>
            {
                var parts = x.Split('|');
                return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
            }).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
