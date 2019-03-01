using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace TyniBot
{
    public class DefaultHandler : IChannelHandler
    {
        protected IDiscordClient Client;
        protected CommandService Commands;
        protected ServiceProvider Services;

        public DefaultHandler(IDiscordClient client, ServiceProvider services)
        {
            Client = client;
            Services = services;

            Commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Commands.AddModuleAsync(typeof(Ping), services).Wait();
            Commands.AddModuleAsync(typeof(Clear), services).Wait();
            Commands.AddModuleAsync(typeof(MafiaCommand), services).Wait();
        }

        public virtual async Task<IResult> MessageReceived(TyniCommandContext context)
        {
            return await TryParseCommand(context);
        }

        public async Task<IResult> TryParseCommand(TyniCommandContext context)
        {
            var message = context.Message;

            int cmdPos = 0;
            if (!(message.HasCharPrefix('!', ref cmdPos) || message.HasMentionPrefix(Client.CurrentUser, ref cmdPos))) return ExecuteResult.FromSuccess();

            return await Commands.ExecuteAsync(context, cmdPos, Services);
        }
    }
}