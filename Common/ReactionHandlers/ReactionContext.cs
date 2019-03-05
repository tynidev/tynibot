using Discord;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace TyniBot
{
    public class ReactionContext : BotContext
    {
        public SocketGuild Guild { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketUser User { get; }
        public SocketUserMessage Message { get; }
        public bool IsPrivate => Channel is IPrivateChannel;
        public ReactionContext(BotContext botContext, SocketUserMessage msg, SocketUser user = null)
            : base(botContext.Client, botContext.Database, botContext.Settings)
        {
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = user ?? msg.Author;
            Message = msg;
        }
    }
}
