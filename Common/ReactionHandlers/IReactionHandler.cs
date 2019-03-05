using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System.Threading.Tasks;

namespace TyniBot
{
    public interface IReactionHandler
    {
        [BsonId]
        ulong MsgId { get; }
        Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded);
        Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved);
        Task ReactionsCleared(ReactionContext context);
    }
}