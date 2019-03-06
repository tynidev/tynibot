using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Text;
using System.Threading.Tasks;
using TyniBot;

namespace Discord.Mafia
{
    public class ScoringHandler : IReactionHandler
    {
        [BsonId]
        public ulong MsgId { get; set; }

        public ulong GameId { get; set; }

        public ScoringHandler() { }

        public Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            var user = reactionAdded.User.Value;
            if (user.IsBot || user.IsWebhook) return Task.CompletedTask;

            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            // TODO: udpate score
            return Task.CompletedTask;
        }

        public Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            var user = reactionRemoved.User.Value;
            if (user.IsBot || user.IsWebhook) return Task.CompletedTask;

            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            // TODO: update score
            return Task.FromResult<IResult>(null);
        }

        public Task ReactionsCleared(ReactionContext context)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            return Task.FromResult<IResult>(null);
        }

        private string GetUnicodeString(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                sb.Append("\\u");
                sb.Append(String.Format("{0:x4}", (int)c));
            }
            return sb.ToString();
        }
    }
}
