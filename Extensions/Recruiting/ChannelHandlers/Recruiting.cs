using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TyniBot;

namespace Discord.Recruiting
{
    public class Recruiting : DefaultHandler
    {
        public Recruiting(IDiscordClient client, ServiceProvider services) : base(client, services) { }

        public override async Task<IResult> MessageReceived(TyniBot.CommandContext context)
        {
            var message = context.Message;
            var channel = context.Channel;

            KeyValuePair<string, string> addition = ParseMsg(message);
            await message.DeleteAsync();

            var boardMsg = (await channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();

            Dictionary<string, string> board = StringToBoard(boardMsg.Content);

            if (board.ContainsKey(addition.Key))
            {
                board[addition.Key] = addition.Value;
            }
            else
            {
                board.Add(addition.Key, addition.Value);
            }

            await boardMsg.DeleteAsync();

            await channel.SendMessageAsync(BoardToString(board));

            return ExecuteResult.FromSuccess();
        }

        private KeyValuePair<string, string> ParseMsg(IUserMessage message)
        {
            Uri uri = new Uri(message.Content);
            return new KeyValuePair<string, string>(message.Author.Username, uri.AbsoluteUri);
        }

        private Dictionary<string, string> StringToBoard(string content)
        {
            var lines = Regex.Split(content, "\r\n|\r|\n");
            return lines.Select(x =>
            {
                var parts = x.Split('|');
                return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
            }).ToDictionary(x => x.Key, x => x.Value); 
        }

        private string BoardToString(Dictionary<string, string> board)
        {
            var pairs = board.OrderBy(x => x.Key).Select(x => $"{x.Key} | {x.Value}");
            return string.Join("\r\n", pairs);
        }
    }
}
