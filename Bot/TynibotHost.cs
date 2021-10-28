using Discord;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.Bot;
using TyniBot.Commands;
using TyniBot.Models;

namespace TyniBot
{
    public class TynibotHost
    {
        private DiscordSocketClient Client;
        private ServiceProvider Services;
        private BotSettings Settings = null;
        private LiteDatabase Database;
        private BotContext Context = null;

        private DefaultHandler DefaultHandler = null;
        private readonly Dictionary<string, IChannelHandler> ChannelHandlers = new Dictionary<string, IChannelHandler>();
        private readonly Dictionary<string, SlashCommand> SlashCommands = new Dictionary<string, SlashCommand>()
        {
            { "ping", new PingSlashCommand() }
        };

        public async Task RunAsync(
            BotSettings settings,
            Func<LogMessage, Task> logFunction,
            CancellationToken? stoppingToken = null)
        {
            this.Settings = settings;

            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug
            });

            Services = new ServiceCollection().BuildServiceProvider();

            using (Database = new LiteDatabase(@"tynibotdata.db")) // DB for long term state
            {
                Context = new BotContext(Client, Database, this.Settings);

                DefaultHandler = new DefaultHandler(Client, Services, new List<Type>());

                var DefaultCommands = new List<Type>()
                {
                    typeof(Ping),
                    typeof(Version),
                    //typeof(Clear),
                    typeof(Discord.Mafia.MafiaCommand),
                    typeof(Discord.Matches.MatchesCommand),
                    typeof(Discord.Inhouse.InhouseCommand)
                };

                foreach (var type in DefaultCommands)
                    DefaultHandler.Commands.AddModuleAsync(type, Services).Wait();
                
                foreach (var slashCommand in SlashCommands.Values)
                {
                    await Client.CreateGlobalApplicationCommandAsync(slashCommand.CreateSlashCommand());
                }
                // TODO: Dynamically load these from DLLs
                //ChannelHandlers.Add("recruiting", new Discord.Recruiting.Recruiting(Client, Services));

                Client.Log += logFunction;

                Client.MessageReceived += MessageReceived;
                Client.ReactionAdded += ReactionAddedAsync;
                Client.ReactionRemoved += ReactionRemovedAsync;
                Client.ReactionsCleared += ReactionsClearedAsync;
                Client.UserJoined += AnnounceJoinedUser;
                Client.SlashCommandExecuted += SlashCommandTriggeredAsync;

                await Client.LoginAsync(TokenType.Bot, this.Settings.BotToken);
                await Client.StartAsync();

                if (!stoppingToken.HasValue)
                {
                    await Task.Delay(-1); // Wait forever
                }
                else
                {
                    // Wait until cancellation is requested.
                    while (!stoppingToken.Value.IsCancellationRequested)
                    {
                        await Task.Delay(int.MaxValue, stoppingToken.Value);
                    }
                }
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
            if (msg is not SocketUserMessage message) return; // We only accept SocketUserMessages

            if (message.Author.IsBot) return; // We don't allow bots to talk to each other lest they take over the world!

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;

            var context = new CommandContext(Context, message);
            if (context == null || string.IsNullOrWhiteSpace(context.Message.Content)) return; // Context must be valid and message must not be empty

            await handler.MessageReceived(context);
        }

        private async Task ReactionsClearedAsync(Cacheable<IUserMessage, ulong> cachedMsg, Cacheable<IMessageChannel, ulong> channel)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionsCleared(context);
        }

        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMsg, Cacheable<IMessageChannel, ulong> channel, SocketReaction removedReaction)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionRemoved(context, removedReaction);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMsg, Cacheable<IMessageChannel, ulong> channel, SocketReaction addedReaction)
        {
            var msg = await cachedMsg.DownloadAsync();
            if (msg == null) return;

            IChannelHandler handler = ChannelHandlers.ContainsKey(msg.Channel.Name) ? ChannelHandlers[msg.Channel.Name] : DefaultHandler;
            var context = new ReactionContext(Context, msg);

            await handler.ReactionAdded(context, addedReaction);
        }

        private async Task SlashCommandTriggeredAsync(SocketSlashCommand command)
        {
            if (SlashCommands.TryGetValue(command.Data.Name, out SlashCommand slashCommand))
            {
                await slashCommand.HandleCommandAsync(command);
            }

            await command.RespondAsync("Invalid command", ephemeral: true);
        }

        #endregion
    }
}
