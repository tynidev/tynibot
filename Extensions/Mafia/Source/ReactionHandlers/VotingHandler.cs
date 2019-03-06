using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot;

namespace Discord.Mafia
{
    public class VotingHandler : IReactionHandler
    {
        [BsonId]
        public ulong MsgId { get; set; }

        public ulong GameId { get; set; }

        public VotingHandler() { }

        public Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);
            var votedPlayer = game.Players.Where(p => p.Value.Emjoi == reactionAdded.Emote.Name).First().Value;

            if(votedPlayer.Id == reactionAdded.UserId || // we don't allow you to vote for yourself
               (game.Votes.ContainsKey(reactionAdded.UserId) && game.Votes[reactionAdded.UserId].Length >= game.Mafia.Count)) // we don't allow more than mafia votes
            {
                context.Message.RemoveReactionAsync(reactionAdded.Emote, reactionAdded.User.Value);
                return Task.CompletedTask;
            }

            game.AddVote(reactionAdded.UserId, votedPlayer.Id);
            games.Update(game);
            
            return Task.CompletedTask;
        }

        public Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);
            var votedPlayer = game.Players.Where(p => p.Value.Emjoi == reactionRemoved.Emote.Name).First().Value;

            game.RemoveVote(reactionRemoved.UserId, votedPlayer.Id);
            games.Update(game);

            return Task.CompletedTask;
        }

        public Task ReactionsCleared(ReactionContext context)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            return Task.CompletedTask;
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
