using Discord.WebSocket;
using LiteDB;
using System.Linq;
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
        private IUser UserReacted;
        private Player PlayerVoted;
        private Game Game;
        private LiteCollection<Game> Games;

        [BsonIgnore]
        public static bool PrivateVoting { get { return false; } }

        #region IReactionHandler

        public async Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            if(!(await ValidateAsync(context, reactionAdded)) || // do we have everything we need
               !Game.AddVote(UserReacted.Id, PlayerVoted.Id))
            {
                if (!UserReacted.IsBot) // we don't need to remove messages we ourselves put there
                {
                    if(PrivateVoting)
                    {
                        await context.Channel.SendMessageAsync($"Vote is invalid and will be ignored. You can not vote for yourself and you can only vote {Game.Mafia.Count} times.");
                    }
                    else
                    {
                        await context.Message.RemoveReactionAsync(reactionAdded.Emote, reactionAdded.User.Value);
                    }
                }
                return;
            }

            Games.Update(Game);

            if (Game.Score())
            {
                Games.Update(Game);
                await Output.Score(Game, Context.Channel);
            }
        }

        public async Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            if (!(await ValidateAsync(context, reactionRemoved)))
                return;   

            Game.RemoveVote(reactionRemoved.UserId, PlayerVoted.Id);
            Games.Update(Game);

            return;
        }

        public Task ReactionsCleared(ReactionContext context)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private async Task<bool> ValidateAsync(ReactionContext context, SocketReaction reaction)
        {
            if (context == null || reaction == null || !reaction.User.IsSpecified) return false;

            Context = context;

            UserReacted = reaction.User.Value;

            if (UserReacted.IsBot || UserReacted.IsWebhook) return false;

            Games = Context.Database.GetCollection<Game>();

            Game = await Game.GetGameAsync(this.GameId, Context.Client, Games);

            PlayerVoted = Game.Players.Where(p => p.Value.Emoji == reaction.Emote.Name).First().Value;
            return true;
        }

        #endregion
    }
}
