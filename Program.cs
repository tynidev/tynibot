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

namespace TyniBot
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private ServiceProvider Services;

        private string SettingsPath => $"{AssemblyDirectory}/botsettings.json";
        private BotSettings Settings = null;

        private Dictionary<string, IChannelHandler> ChannelListeners = new Dictionary<string, IChannelHandler>()
        {
            { "recruiting", new Recruiting() }
        };

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug
            });

            Commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Services = new ServiceCollection().BuildServiceProvider();

            Client.Log += Log;
            Client.MessageReceived += Client_MessageReceived;
            await Commands.AddModuleAsync(typeof(Ping), Services);

            Settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath));

            await Client.LoginAsync(TokenType.Bot, Settings.BotToken);
            await Client.StartAsync();

            await Task.Delay(-1); // Wait forever
        }

        private async Task Client_MessageReceived(SocketMessage msg)
        {
            var message = msg as SocketUserMessage;
            if (message == null) return;

            if (message.Author.IsBot) return;

            var context = new CommandContext(Client, message);
            if (context == null || string.IsNullOrWhiteSpace(context.Message.Content)) return;

            // Do we have a channel listener?
            if (ChannelListeners.ContainsKey(msg.Channel.Name))
            {
                await ChannelListeners[msg.Channel.Name].Execute(context, Services);
            }
            else // perhaps this is a Command?
            {
                int cmdPos = 0;
                if (!(message.HasCharPrefix('!', ref cmdPos) || message.HasMentionPrefix(Client.CurrentUser, ref cmdPos))) return;

                var result = await Commands.ExecuteAsync(context, cmdPos, Services);
                if (!result.IsSuccess)
                {
                    await Log(new LogMessage(LogSeverity.Error, nameof(Client_MessageReceived), $"Failed to execute command. Input Text: {message.Content} Error: {result.ErrorReason}"));
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
