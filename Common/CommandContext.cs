using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;

namespace TyniBot
{
    public class CommandContext : BotContext, ICommandContext
    {
        public SocketGuild Guild { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketUser User { get; }
        public SocketUserMessage Message { get; }
        public bool IsPrivate => Channel is IPrivateChannel;

        public CommandContext(BotContext botContext, SocketUserMessage msg, SocketUser user = null) :
            base(botContext.Client, botContext.Database, botContext.Settings)
        {
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Channel = msg.Channel;
            User = user ?? msg.Author;
            Message = msg;
        }

        #region ICommandContext
        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
        IMessageChannel ICommandContext.Channel => Channel;
        IUser ICommandContext.User => User;
        IUserMessage ICommandContext.Message => Message;
        #endregion
    }
}
