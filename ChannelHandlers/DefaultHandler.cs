using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace TyniBot
{
    public class DefaultHandler : IChannelHandler
    {
        protected IDiscordClient Client;
        protected CommandService Commands;
        protected ServiceProvider Services;
        protected BotSettings Settings;
        protected LiteDatabase Database;

        public DefaultHandler(IDiscordClient client, ServiceProvider services, LiteDatabase datbase, BotSettings settings)
        {
            Client = client;
            Services = services;
            Settings = settings;

            Commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Commands.AddModuleAsync(typeof(Ping), services).Wait();
        }

        public virtual async Task<IResult> MessageReceived(CommandContext context)
        {
            return await TryParseCommand(context);
        }

        public async Task<IResult> TryParseCommand(CommandContext context)
        {
            var message = context.Message;

            int cmdPos = 0;
            if (!(message.HasCharPrefix('!', ref cmdPos) || message.HasMentionPrefix(Client.CurrentUser, ref cmdPos))) return ExecuteResult.FromSuccess();

            return await Commands.ExecuteAsync(context, cmdPos, Services);
        }
    }
}