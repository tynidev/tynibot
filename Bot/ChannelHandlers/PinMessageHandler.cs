using Discord;
using Discord.Commands;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TyniBot;

namespace TyniBot
{

    public class PinnedMessage
    {
        [BsonId]
        public ulong ChannelId { get; set; }
        public ulong MsgId { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
    }

    public class PinMessageHandler : DefaultHandler
    {

        public PinMessageHandler(IDiscordClient client, ServiceProvider services, List<Type> SupportedCommands) : base(client, services, SupportedCommands)
        {
            base.Commands.AddModuleAsync(typeof(PinMessage), Services).Wait();
        }

        public override async Task<IResult> MessageReceived(CommandContext context)
        {
            if (context.User.IsBot || context.User.IsWebhook) return ExecuteResult.FromSuccess(); // ignore bot messages

            var msgCollection = context.Database.GetCollection<PinnedMessage>();
            var pinnedMsg = msgCollection.Find(m => m.ChannelId == context.Channel.Id).FirstOrDefault();

            if(pinnedMsg == null) // no message has been pinned yet
            {
                return await base.MessageReceived(context); // proceed as normal
            }
            else // we have a pinned message
            {
                // is this the unpin command?
                var result = await base.MessageReceived(context);
                if(result.IsSuccess) // if we processed a command then return
                    return result;

                // delete last pinned message
                await context.Channel.DeleteMessageAsync(pinnedMsg.MsgId);

                // write pinned message again
                var newPin = await context.Channel.SendMessageAsync($"**Pinned Message from: {pinnedMsg.Author}\n\n**{pinnedMsg.Content}");

                // update database
                pinnedMsg.MsgId = newPin.Id;
                msgCollection.Update(pinnedMsg);
                return ExecuteResult.FromSuccess();
            }
        }
    }
}
