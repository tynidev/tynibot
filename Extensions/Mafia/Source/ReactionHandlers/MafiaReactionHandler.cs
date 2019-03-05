using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System.Threading.Tasks;
using TyniBot;

namespace Discord.Mafia
{
    public class MafiaReactionHandler : IReactionHandler
    {
        [BsonId]
        public ulong MsgId { get; set; }

        public MafiaReactionHandler() { }

        public Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            return Task.FromResult<IResult>(null);
        }

        public Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            return Task.FromResult<IResult>(null);
        }

        public Task ReactionsCleared(ReactionContext context)
        {
            return Task.FromResult<IResult>(null);
        }
    }
}
