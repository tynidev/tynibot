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
                DefaultHandler = new DefaultHandler(Client, Services);
                ChannelHandlers.Add("recruiting", new Recruiting(Client, Services));

                Client.Log += Log;
                Client.MessageReceived += MessageReceived;

                await Client.LoginAsync(TokenType.Bot, Settings.BotToken);
                await Client.StartAsync();
                await Task.Delay(-1); // Wait forever
            }
        }

        #region EventHandlers

        private async Task MessageReceived(SocketMessage msg)
        {
            // Take input and Validate
            var message = msg as SocketUserMessage;
            if (message == null) return; // We only accept SocketUserMessages

            if (message.Author.IsBot) return; // We don't allow bots to talk to each other lest they take over the world!

            var context = new TyniCommandContext(Client, Database, Settings, message);
            if (context == null || string.IsNullOrWhiteSpace(context.Message.Content)) return; // Context must be valid and message must not be empty

            // Do we have a custom channel listener?
            if (ChannelHandlers.ContainsKey(msg.Channel.Name))
            {
                await ChannelHandlers[msg.Channel.Name].MessageReceived(context);
            }
            // else Use the DefaultHandler
            else
            {
                await DefaultHandler.MessageReceived(context);
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        #endregion
    }
}
