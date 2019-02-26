using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;

namespace TyniBot
{
    public class TyniCommandContext : ICommandContext
    {
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketUser User { get; }
        public SocketUserMessage Message { get; }
        public LiteDatabase Database { get; }
        public BotSettings Settings { get; }

        public bool IsPrivate => Channel is IPrivateChannel;

        public TyniCommandContext(DiscordSocketClient client, LiteDatabase database, BotSettings settings, SocketUserMessage msg, SocketUser user = null)
        {
            Client = client;
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Database = database;
            Settings = settings;
            Channel = msg.Channel;
            User = user ?? msg.Author;
            Message = msg;
        }

        //ICommandContext
        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
        IMessageChannel ICommandContext.Channel => Channel;
        IUser ICommandContext.User => User;
        IUserMessage ICommandContext.Message => Message;

    }
}
