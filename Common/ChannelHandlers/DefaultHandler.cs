using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace TyniBot
{
    public class DefaultHandler : IChannelHandler
    {
        protected IDiscordClient Client;
        public CommandService Commands { get; }
        protected ServiceProvider Services;

        public DefaultHandler(IDiscordClient client, ServiceProvider services, List<Type> SupportedCommands = null)
        {
            Client = client;
            Services = services;

            Commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            if (SupportedCommands != null)
                foreach (var type in SupportedCommands)
                    Commands.AddModuleAsync(type, Services).Wait();
        }

        public virtual async Task<IResult> MessageReceived(CommandContext context)
        {
            var message = context.Message;

            int cmdPos = 0;
            if (!(message.HasCharPrefix('!', ref cmdPos) || message.HasMentionPrefix(Client.CurrentUser, ref cmdPos))) return ExecuteResult.FromError(CommandError.UnknownCommand, "");

            return await Commands.ExecuteAsync(context, cmdPos, Services);
        }

        public virtual async Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            var col = context.Database.GetCollection<IReactionHandler>();
            var handler = col.Find(rh => rh.MsgId == context.Message.Id).FirstOrDefault();

            if (handler == null) return;

            await handler.ReactionAdded(context, reactionAdded);
        }

        public virtual async Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            var col = context.Database.GetCollection<IReactionHandler>();
            var handler = col.Find(rh => rh.MsgId == context.Message.Id).FirstOrDefault();

            if (handler == null) return;

            await handler.ReactionRemoved(context, reactionRemoved);
        }

        public virtual async Task ReactionsCleared(ReactionContext context)
        {
            var col = context.Database.GetCollection<IReactionHandler>();
            var handler = col.Find(rh => rh.MsgId == context.Message.Id).FirstOrDefault();

            if (handler == null) return;

            await handler.ReactionsCleared(context);
        }
    }
}