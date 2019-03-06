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

        private ReactionContext Context;

        public VotingHandler() { }

        public async Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            Context = context;
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);
            var votedPlayer = game.Players.Where(p => p.Value.Emoji == reactionAdded.Emote.Name).First().Value;

            if(votedPlayer.Id == reactionAdded.UserId || // we don't allow you to vote for yourself
               (game.Votes.ContainsKey(reactionAdded.UserId) && game.Votes[reactionAdded.UserId].Length >= game.Mafia.Count)) // we don't allow more than mafia votes
            {
                await context.Message.RemoveReactionAsync(reactionAdded.Emote, reactionAdded.User.Value);
                return;
            }

            game.AddVote(reactionAdded.UserId, votedPlayer.Id);
            games.Update(game);

            if(game.Score())
            {
                await OutputGameEnd(game);
            }
        }

        public async Task OutputGameEnd(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = game.Players.OrderByDescending(x => x.Value.Score);

            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(p => $"{p.Value.Mention} = {p.Value.Score}")));

            embedBuilder.AddField("Mafia: ", string.Join(' ', game.Mafia.Select(u => u.Mention)));
            if (game.Joker != null)
                embedBuilder.AddField("Joker: ", game.Joker.Mention);

            await Context.Channel.SendMessageAsync($"**Game Over! {ordered.First().Value.Mention} Won!**", false, embedBuilder.Build());
        }

        public Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);
            var votedPlayer = game.Players.Where(p => p.Value.Emoji == reactionRemoved.Emote.Name).First().Value;

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
    }
}
