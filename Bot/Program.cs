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
using System.Threading;

namespace TyniBot
{
    public class Program
    {
        private static string AssemblyDirectory
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(location);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        private static string SettingsPath => $"{AssemblyDirectory}/botsettings.json";

        static void Main(string[] args) {
            BotSettings settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText(SettingsPath));

            TynibotHost botHost = new TynibotHost();
            botHost.RunAsync(settings, Log).GetAwaiter().GetResult();
        }

        private static Func<LogMessage, Task> Log = (msg) =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };
    }
}
