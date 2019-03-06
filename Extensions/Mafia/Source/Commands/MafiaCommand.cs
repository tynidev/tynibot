using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LiteDB;
using TyniBot;
using Discord.WebSocket;

namespace Discord.Mafia
{
    public class MafiaCommand : ModuleBase<TyniBot.CommandContext>
    {
        #region Commands
        [Command("mafia"), Summary("**!mafia <?gameMode=default(battle|joker|default)> <?numOfMafias=1> <@player1> <@player2>** Creates a game of Mafia!")]
        public async Task NewGameCommand(int numMafias, string gameMode, [Remainder]string message = "") // matches | new 2 @Mentions | new 2 j @Mentions
        {
            await CreateGame(numMafias, gameMode);
        }

        [Command("mafia")]
        public async Task NewGameCommand(string gameMode, int numMafias, [Remainder]string message = "") // matches | new j 2 @Mentions
        {
            await CreateGame(numMafias, gameMode);
        }

        [Command("mafia")]
        public async Task NewGameCommand(string gameMode, [Remainder]string message = "") // matches | new @Mentions | new j @Mentions
        {
            if (gameMode.ToLower() == "help")
            {
                await HelpCommand();
                return;
            }
            
            await CreateGame(1, gameMode);
        }

        [Command("mafia"), Summary("**!mafia help** | Displays this help text.")]
        public async Task HelpCommand()
        {
            var commands = typeof(MafiaCommand).GetMethods()
                      .Where(m => m.GetCustomAttributes(typeof(SummaryAttribute), false).Length > 0)
                      .ToArray();

            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (var command in commands)
            {
                var name = (CommandAttribute)command.GetCustomAttributes(typeof(CommandAttribute), false)[0];
                var summary = (SummaryAttribute)command.GetCustomAttributes(typeof(SummaryAttribute), false)[0];
                // Get the command Summary attribute information
                string embedFieldText = summary.Text ?? "No description available\n";

                embedBuilder.AddField(name.Text, embedFieldText);
            }

            await ReplyAsync("**Mafia Commands:** ", false, embedBuilder.Build());
        }
        #endregion

        #region Helpers
        private async Task CreateGame(int numMafias, string gameMode = "default")
        {
            Mafia.GameMode mode = GameMode.Normal;
            switch(gameMode.ToLower())
            {
                case "b":
                case "battle":
                    mode = GameMode.Battle;
                    break;
                case "j":
                case "joker":
                    mode = GameMode.Joker;
                    break;
            }

            Game game;
            try
            {
                game = Mafia.Game.CreateGame(Context.Message.MentionedUsers.Select(s => (IUser)s).ToList(), numMafias, mode);

                // Set game id to ChannelId
                game.Id = Context.Channel.Id;
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            var games = Context.Database.GetCollection<Game>();

            // Delete current game if exists
            try
            {
                var existingGame = GetGame(Context.Channel.Id, Context.Guild.GetUser, games);
                if (existingGame != null)
                    games.Delete(g => g.Id == existingGame.Id);
            }
            catch { }

            // Notify each Villager
            foreach (var user in game.Villagers)
                await user.SendMessageAsync("You are a Villager!");

            // Notify each Mafia
            foreach (var user in game.Mafia)
                await user.SendMessageAsync("You are in the Mafia!");

            // Notify each Joker
            if (game.Joker != null)
                await game.Joker.SendMessageAsync("You are the Joker!");

            await OutputGameSummary(game);

            var reactionHandlers = Context.Database.GetCollection<IReactionHandler>();

            IUserMessage votingMessage = await OutputVotingMessage(game);
            IUserMessage scoringMessage = await OutputScoringMessage(game);

            List<IEmote> reactions = new List<IEmote>();
            foreach (var p in game.Players)
            {
                reactions.Add(new Emoji(p.Value.Emjoi));
            }
            await votingMessage.AddReactionsAsync(reactions.ToArray());

            reactions = new List<IEmote>() { new Emoji("\ud83d\udd36"), new Emoji("\ud83d\udd37"), new Emoji("➕") };
            await scoringMessage.AddReactionsAsync(reactions.ToArray());

            // Insert into DB
            games.Insert(game);
            games.EnsureIndex(x => x.Id);

            reactionHandlers.Insert(new VotingHandler() { MsgId = votingMessage.Id, GameId = game.Id });
            reactionHandlers.Insert(new ScoringHandler() { MsgId = scoringMessage.Id, GameId = game.Id });

            reactionHandlers.EnsureIndex(x => x.MsgId);
        }

        private async Task<IUserMessage> OutputVotingMessage(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            int i = 0;
            string players = "";
            string[] emojis = new string[] { "1\u20e3", "2\u20e3", "3\u20e3", "4\u20e3", "5\u20e3", "6\u20e3", "7\u20e3", "8\u20e3" };
            foreach (var p in game.Players.Values)
            {
                p.Emjoi = emojis[i++];
                players += $"{p.Emjoi} - {p.Mention}\r\n";
            }

            embedBuilder.AddField("Players", players);

            return await ReplyAsync($"**Vote for Mafia!**", false, embedBuilder.Build());
        }

        private async Task<IUserMessage> OutputScoringMessage(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("How to vote", ":large_orange_diamond: Orange Team Won! :large_blue_diamond: Blue Team Won! ::heavy_plus_sign: Game went to OT!");

            return await ReplyAsync($"**Input the final score!**", false, embedBuilder.Build());
        }

        public static async Task OutputGameEnd(Game game, Dictionary<ulong, int> scores, ISocketMessageChannel channel)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = scores.OrderByDescending(x => x.Value);

            embedBuilder.AddField("Mafia: ", string.Join(' ', game.Mafia.Select(u => u.Mention)));
            if(game.Joker != null)
                embedBuilder.AddField("Joker: ", game.Joker.Mention);

            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));

            await channel.SendMessageAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputGameSummary(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            await ReplyAsync($"**New Mafia Game - Mode({game.Mode}), NumMafia({game.Mafia.Count})**", false, embedBuilder.Build());
        }

        public static Game GetGame(ulong id, Func<ulong, IUser> GetUser, LiteCollection<Game> collection)
        {
            var game = collection.FindOne(g => g.Id == id);
            if (game == null)
                throw new KeyNotFoundException();

            game.PopulateUser(GetUser);
            return game;
        }
        #endregion
    }
}
