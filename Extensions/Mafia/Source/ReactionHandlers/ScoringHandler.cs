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
    public class ScoringHandler : IReactionHandler
    {
        [BsonId]
        public ulong MsgId { get; set; }

        public ulong GameId { get; set; }

        private ReactionContext Context;

        public ScoringHandler() { }

        public async Task ReactionAdded(ReactionContext context, SocketReaction reactionAdded)
        {
            Context = context;
            var user = reactionAdded.User.Value;
            if (user.IsBot || user.IsWebhook) return;

            var em = GetUnicodeString(reactionAdded.Emote.Name);

            var games = Context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, Context.Guild.GetUser, games);

            if (game.HostId != user.Id)
            {
                await removeReaction(reactionAdded.Emote.Name, user, game.Id);
                return;
            }

            if (reactionAdded.Emote.Name == Game.OrangeEmoji)
            {
                var metadata = Context.Message.Reactions.Where(e => e.Key.Name == Game.BlueEmoji).First().Value;
                if (metadata.ReactionCount > 1)
                    await removeReaction(Game.BlueEmoji, user, game.Id);
                game.WinningTeam = Team.Orange;
            }
            else if(reactionAdded.Emote.Name == Game.BlueEmoji)
            {
                var metadata = Context.Message.Reactions.Where(e => e.Key.Name == Game.OrangeEmoji).First().Value;
                if (metadata.ReactionCount > 1)
                    await removeReaction(Game.OrangeEmoji, user, game.Id);
                game.WinningTeam = Team.Blue;
            }
            else if(reactionAdded.Emote.Name == Game.OvertimeEmoji)
            {
                game.OvertimeReached = true;
            }
            else if(reactionAdded.Emote.Name == Game.EndedEmoji)
            {
                if (game.WinningTeam == null)
                {
                    await removeReaction(Game.EndedEmoji, user, game.Id);
                    return;
                }

                var reactionHandlers = context.Database.GetCollection<IReactionHandler>();
                reactionHandlers.Delete(u => u.MsgId == this.MsgId); // un register myself // TODO: figure out why delete doesn't work

                IUserMessage votingMessage = await OutputVotingMessage(game);
                List<IEmote> reactions = new List<IEmote>();
                foreach (var p in game.Players)
                {
                    reactions.Add(new Emoji(p.Value.Emoji));
                }
                await votingMessage.AddReactionsAsync(reactions.ToArray());

                reactionHandlers.Insert(new VotingHandler() { MsgId = votingMessage.Id, GameId = game.Id });
            }
            
            games.Update(game);
        }

        public async Task ReactionRemoved(ReactionContext context, SocketReaction reactionRemoved)
        {
            Context = context;
            var user = reactionRemoved.User.Value;
            if (user.IsBot || user.IsWebhook) return;

            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            if (game.HostId != user.Id)
            {
                await removeReaction(reactionRemoved.Emote.Name, user, game.Id);
                return;
            }

            var blueCount = Context.Message.Reactions.Where(e => e.Key.Name == Game.BlueEmoji).First().Value.ReactionCount;
            var orangeCount = Context.Message.Reactions.Where(e => e.Key.Name == Game.OrangeEmoji).First().Value.ReactionCount;

            if (blueCount + orangeCount == 2)
                game.WinningTeam = null;
            else if (reactionRemoved.Emote.Name == Game.OvertimeEmoji)
                game.OvertimeReached = false;

            games.Update(game);
            return;
        }

        public Task ReactionsCleared(ReactionContext context)
        {
            var games = context.Database.GetCollection<Game>();
            var game = MafiaCommand.GetGame(this.GameId, context.Guild.GetUser, games);

            return Task.CompletedTask;
        }

        private async Task<IUserMessage> OutputVotingMessage(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            int i = 0;
            string players = "";
            string[] emojis = new string[] { "1\u20e3", "2\u20e3", "3\u20e3", "4\u20e3", "5\u20e3", "6\u20e3", "7\u20e3", "8\u20e3" };
            foreach (var p in game.Players.Values)
            {
                p.Emoji = emojis[i++];
                players += $"{p.Emoji} - {p.Mention} ";
                if (i > 0 && i % 3 == 0) players += "\r\n";
            }

            embedBuilder.AddField("Players:", players);

            return await Context.Channel.SendMessageAsync($"**Vote for Mafia!**", false, embedBuilder.Build());
        }

        private async Task removeReaction(string emoji, IUser user, ulong gameId)
        {
            await Context.Message.RemoveReactionAsync(new Emoji(emoji), user);
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
