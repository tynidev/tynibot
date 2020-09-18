using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using LiteDB;

namespace TyniBot
{
    class Program
    {
        private DiscordSocketClient Client;
        private ServiceProvider Services;
        private BotSettings Settings = null;
        private LiteDatabase Database;
        private BotContext Context = null;

        private DefaultHandler DefaultHandler = null;
        private Dictionary<string, IChannelHandler> ChannelHandlers = new Dictionary<string, IChannelHandler>();

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        private string SettingsPath => $"{AssemblyDirectory}/botsettings.json";

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            Settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath));

            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug
            });

            Services = new ServiceCollection().BuildServiceProvider();

            using (Database = new LiteDatabase(@"tynibotdata.db")) // DB for long term state
            {
                Context = new BotContext(Client, Database, Settings); 

                DefaultHandler = new DefaultHandler(Client, Services, new List<Type>());

                var DefaultCommands = new List<Type>()
                {
                    typeof(Ping),
                    typeof(Clear),
                    typeof(Discord.Mafia.MafiaCommand),
                    typeof(Discord.Matches.MatchesCommand),
                    typeof(Discord.Inhouse.InhouseCommand)
                };

                foreach(var type in DefaultCommands)
                    DefaultHandler.Commands.AddModuleAsync(type, Services).Wait();

                // TODO: Dynamically load these from DLLs
                ChannelHandlers.Add("recruiting", new Discord.Recruiting.Recruiting(Client, Services));

                Client.Log += Log;
                Client.MessageReceived += MessageReceived;
                Client.ReactionAdded += ReactionAddedAsync;
                Client.ReactionRemoved += ReactionRemovedAsync;
                Client.ReactionsCleared += ReactionsClearedAsync;
                Client.UserJoined += AnnounceJoinedUser;

                await Client.LoginAsync(TokenType.Bot, Settings.BotToken);
                await Client.StartAsync();
                await Task.Delay(-1); // Wait forever
            }
        }
        public async Task AnnounceJoinedUser(SocketGuildUser user) //Welcomes the new user
        {
            var channel = Client.GetChannel(124366291611025417) as SocketTextChannel; // Gets the channel to send the message in
            await channel.SendMessageAsync($"Welcome {user.Mention} to {channel.Guild.Name}. Please wait while we load the real humans. For general guidance in the meantime, check out <#549039583459934209>"); //Welcomes the new user
        }

        #region EventHandlers

        private async Task MessageReceived(SocketMessage msg)
        {
            // Take input and Validate
            var message = msg as SocketUserMessage;
            if (message == null) return; // We only accept SocketUserMessages

            if (message.Author.IsBot) return; // We don't allow bots to talk to each other lest they take over the world!

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;

            var context = new CommandContext(Context, message);
            if (context == null || string.IsNullOrWhiteSpace(context.Message.Content)) return; // Context must be valid and message must not be empty

            await handler.MessageReceived(context);
        }

        private async Task ReactionsClearedAsync(Cacheable<IUserMessage, ulong> cachedMsg, ISocketMessageChannel channel)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionsCleared(context);
        }

        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMsg, ISocketMessageChannel channel, SocketReaction removedReaction)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionRemoved(context, removedReaction);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMsg, ISocketMessageChannel channel, SocketReaction addedReaction)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionAdded(context, addedReaction);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        #endregion
    }
}
