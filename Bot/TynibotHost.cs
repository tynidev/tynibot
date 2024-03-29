using Discord;
using Discord.Bot;
using Discord.Bot.Utils;
using Discord.Cea;
using Discord.Inhouse;
using Discord.Mafia;
using Discord.Matches;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TyniBot.Commands;
using TyniBot.Discord.Bot.Extensions.Cea.Utils;

namespace TyniBot
{
    public class TynibotHost
    {
        private DiscordSocketClient Client;
        private ServiceProvider Services;
        private BotSettings Settings = null;
        private LiteDatabase Database;
        private StorageClient StorageClient;
        private BotContext Context = null;

        private DefaultHandler DefaultHandler = null;
        private readonly Dictionary<string, IChannelHandler> ChannelHandlers = new Dictionary<string, IChannelHandler>();

        private readonly List<ApplicationCommand> ApplicationCommands = new List<ApplicationCommand>()
        {
            new PingSlashCommand(),
            new VersionSlashCommand(),
            new RecruitingCommand(),
            new AdminRecruitingCommand(),
        };

        private readonly Dictionary<string, SlashCommand> SlashCommandDictionary;
        private readonly Dictionary<string, UserCommand> UserCommandDictionary;

        public TynibotHost()
        {
            SlashCommandDictionary = ApplicationCommands.Where(c => c is SlashCommand).Select(c => c as SlashCommand).ToDictionary(s => s.Name);
            UserCommandDictionary = ApplicationCommands.Where(c => c is UserCommand).Select(c => c as UserCommand).ToDictionary(u => u.Name);
        }

        public async Task RunAsync(
            BotSettings settings,
            Func<string, Task> logFunction,
            CancellationToken? stoppingToken = null)
        {
            this.Settings = settings;

            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug,
                UseInteractionSnowflakeDate = true,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
            });
            StorageClient = new StorageClient(this.Settings.StorageConnectionString);

            Services = new ServiceCollection().BuildServiceProvider();

            using (Database = new LiteDatabase(@"tynibotdata.db")) // DB for long term state
            {
                Context = new BotContext(Client, Database, this.Settings);

                DefaultHandler = new DefaultHandler(Client, Services, new List<Type>());

                var DefaultCommands = new List<Type>()
                {
                    //typeof(Clear),
                    typeof(MafiaCommand),
                    typeof(MatchesCommand),
                    typeof(InhouseCommand),
                };

                foreach (var type in DefaultCommands)
                    DefaultHandler.Commands.AddModuleAsync(type, Services).Wait();

                // TODO: Dynamically load these from DLLs
                //ChannelHandlers.Add("recruiting", new Discord.Recruiting.Recruiting(Client, Services));

                Client.Log += async (dMsg) => await logFunction(dMsg.ToString());
                Client.MessageReceived += MessageReceived;
                Client.ReactionAdded += ReactionAddedAsync;
                Client.ReactionRemoved += ReactionRemovedAsync;
                Client.ReactionsCleared += ReactionsClearedAsync;
                Client.UserJoined += AnnounceJoinedUser;
                Client.SlashCommandExecuted += SlashCommandTriggeredAsync;
                Client.UserCommandExecuted += UserCommandTriggeredAsync;
                Client.Ready += ReadyAsync;
                await Client.LoginAsync(TokenType.Bot, this.Settings.BotToken);
                await Client.StartAsync();

                try
                {
                    PlayCEASharp.Configuration.CeaSharpLogging.logger = new CeaLogger(logFunction);
                    // Bootstrap the CEA Data (Otherwise first response will timeout)
                    PlayCEASharp.RequestManagement.LeagueManager.Bootstrap();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Error starting bot: {e}");
                }

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
            var channel = user.Guild.Id == 124366291611025417 ? user.Guild.GetTextChannel(124366291611025417) : user.Guild.DefaultChannel; // Gets the channel to send the message in

            var guild = await Guild.GetGuildAsync(user.Guild.Id, StorageClient);
            var rolesMessage = guild.RolesChannelId != default ? $"Check out <#{guild.RolesChannelId}> to get notifications for when people are looking for others to play with. " : "";
            var inhouseMessage = guild.InhouseChannelId != default ? $"If you are looking for others to play with you can ping in <#{guild.InhouseChannelId}> and hop into a voice channel. " : "";
            var recruitingMessage = guild.RecruitingChannelId != default? $"If you are a Microsoft employee you can get verified. If you are interested in participating in upcoming CEA seasons, head over to <#{guild.RecruitingChannelId}>." : "";

            await channel.SendMessageAsync($"Welcome {user.Mention} to {channel.Guild.Name}. {rolesMessage}{inhouseMessage}{recruitingMessage}"); //Welcomes the new user
        }

        #region EventHandlers

        private async Task ReadyAsync()
        {
            //uncomment to remove all commands before reregistering
            /* await Client.Rest.DeleteAllGlobalCommandsAsync();

            var guilds = await Client.Rest.GetGuildsAsync();

            foreach (var guild in guilds)
            {
                var commands = await Client.Rest.GetGuildApplicationCommands(guild.Id);

                foreach (var command in commands)
                {
                    await command.DeleteAsync();
                }
            }
            */
            

            foreach (var applicationCommand in ApplicationCommands)
            {
                if (applicationCommand.IsGlobal)
                {
                    await Client.Rest.CreateGlobalCommand(applicationCommand.Build());
                }
                else
                {
                    foreach ((var guildId, var permissions) in applicationCommand.GuildIdsAndPermissions)
                    {
                        try
                        {
                            var createdCommand = await Client.Rest.CreateGuildCommand(applicationCommand.Build(), guildId);

                            if (permissions.Count > 0)
                            {
                                await createdCommand.ModifyCommandPermissions(permissions.ToArray());
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Error creating command {applicationCommand.Name} in guilld {guildId}: {e.Message}");
                        }
                    }
                }
            }
        }

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
            var guild = await Guild.GetGuildAsync(command.GuildId.Value, StorageClient);

            if (SlashCommandDictionary.TryGetValue(command.Data.Name, out SlashCommand slashCommand))
            {
                await slashCommand.HandleCommandAsync(command, Client, StorageClient);
            }
            else
            {
                await command.RespondAsync("Invalid command", ephemeral: true);
            }
        }

        private async Task UserCommandTriggeredAsync(SocketUserCommand command)
        {
            

            if (UserCommandDictionary.TryGetValue(command.Data.Name, out UserCommand userCommand))
            {
                await userCommand.HandleCommandAsync(command, Client, StorageClient);
            }
            else
            {
                await command.RespondAsync("Invalid command", ephemeral: true);
            }
        }
        #endregion
    }
}
