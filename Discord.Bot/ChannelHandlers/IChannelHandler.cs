using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Discord.Bot
{
    public interface IChannelHandler
    {
        Task<IResult> MessageReceived(CommandContext context);
        Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded);
        Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved);
        Task ReactionsCleared(ReactionContext context);
    }
}