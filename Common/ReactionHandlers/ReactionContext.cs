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
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Message { get; }
        public bool IsPrivate => Channel is IPrivateChannel;
        public ReactionContext(BotContext botContext, IUserMessage msg)
            : base(botContext.Client, botContext.Database, botContext.Settings)
        {
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = msg.Author;
            Message = msg;
        }
    }
}
