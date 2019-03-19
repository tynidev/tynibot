using Discord.WebSocket;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TyniBot;

namespace Discord.Mafia
{
    public class GameHandler : IReactionHandler
    {
        [BsonId]
        public ulong MsgId { get; set; }
        public ulong GameId { get; set; }

        private ReactionContext Context;
        private IUser UserReacted;
        private Game Game;
        private LiteCollection<Game> Games;
        private LiteCollection<IReactionHandler> ReactionHandlers;

        #region IReactionHandler

        public async Task ReactionAdded(ReactionContext context, SocketReaction reaction)
        {
            if (!(await Validate(context, reaction)))
            {
                if (!UserReacted.IsBot) // we don't need to remove messages we ourselves put there
                    await Context.Message.RemoveReactionAsync(new Emoji(reaction.Emote.Name), reaction.User.Value);
                return;
            }

            if (reaction.Emote.Name == Output.OrangeEmoji) // Which reaction was clicked?
            {
                await SelectWinningTeamAsync(Team.Orange);
            }
            else if (reaction.Emote.Name == Output.BlueEmoji)
            {
                await SelectWinningTeamAsync(Team.Blue);
            }
            else if (reaction.Emote.Name == Output.OvertimeEmoji)
            {
                Game.OvertimeReached = true;
                Games.Update(Game);
            }
            else if (reaction.Emote.Name == Output.EndedEmoji)
            {
                if (Game.WinningTeam == null) // if we don't have a winner remove emoji and return
                {
                    await Context.Message.RemoveReactionAsync(new Emoji(Output.EndedEmoji), UserReacted);
                    return;
                }

                // Un-register this message for receiving new reactions
                ReactionHandlers.Delete(u => u.MsgId == this.MsgId);

                // Output voting notification message
                var votingMessages = await Output.StartVoting(Game, Context.Channel, VotingHandler.PrivateVoting);
                Games.Update(Game); // Update so we store emojis on user

                // Register new message for receiving reactions
                foreach (var votingMessage in votingMessages)
                    ReactionHandlers.Insert(new VotingHandler() { MsgId = votingMessage.Id, GameId = Game.Id });
            }
        }

        public async Task ReactionRemoved(ReactionContext context, SocketReaction reaction)
        {
            if (!(await Validate(context, reaction)))
                return;

            var blueCount = Context.Message.Reactions.Where(e => e.Key.Name == Output.BlueEmoji).First().Value.ReactionCount;
            var orangeCount = Context.Message.Reactions.Where(e => e.Key.Name == Output.OrangeEmoji).First().Value.ReactionCount;

            if (blueCount + orangeCount == 2)
                Game.WinningTeam = null;

            if (reaction.Emote.Name == Output.OvertimeEmoji)
                Game.OvertimeReached = false;

            Games.Update(Game);
            return;
        }

        public Task ReactionsCleared(ReactionContext context) // Should we do anything here?
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private async Task<bool> Validate(ReactionContext context, SocketReaction reaction)
        {
            if (context == null || reaction == null || !reaction.User.IsSpecified) return false;

            Context = context;
            UserReacted = reaction.User.Value;

            if (UserReacted.IsBot || UserReacted.IsWebhook) return false;

            Games = Context.Database.GetCollection<Game>();
            ReactionHandlers = context.Database.GetCollection<IReactionHandler>();

            Game = await Game.GetGameAsync(this.GameId, Context.Client, Games);

            if (UserReacted.Id != Game.HostId) return false;

            return true;
        }

        private async Task SelectWinningTeamAsync(Team winningTeam)
        {
            Game.WinningTeam = winningTeam; // update game first
            Games.Update(Game);

            var losingEmoji = winningTeam == Team.Orange ? Output.BlueEmoji : Output.OrangeEmoji;
            var lostReaction = Context.Message.Reactions.Where(e => e.Key.Name == losingEmoji).First().Value;

            if (lostReaction.ReactionCount > 1)
                await Context.Message.RemoveReactionAsync(new Emoji(losingEmoji), UserReacted);
        }

        #endregion
    }
}
