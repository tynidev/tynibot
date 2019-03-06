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
                game.HostId = Context.User.Id;
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
            foreach (var user in game.Players.Values)
                await user.SendMessageAsync($"You are a {user.Type} on {user.Team} Team!");

            
            var reactions = new List<IEmote>() { new Emoji(Game.OrangeEmoji), new Emoji(Game.BlueEmoji), new Emoji(Game.OvertimeEmoji), new Emoji(Game.EndedEmoji) };

            IUserMessage scoringMessage = await OutputGameSummary(game);
            await scoringMessage.AddReactionsAsync(reactions.ToArray());

            // Insert into DB
            games.Insert(game);
            games.EnsureIndex(x => x.Id);

            var reactionHandlers = Context.Database.GetCollection<IReactionHandler>();
            reactionHandlers.Insert(new ScoringHandler() { MsgId = scoringMessage.Id, GameId = game.Id });
            reactionHandlers.EnsureIndex(x => x.MsgId);
        }

        private async Task<IUserMessage> OutputGameSummary(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Orange Team:", string.Join(' ', game.TeamOrange.Select(u => u.Mention)));
            embedBuilder.AddField("Blue Team:", string.Join(' ', game.TeamBlue.Select(u => u.Mention)));

            embedBuilder.AddField("Game Result:", $"{Game.OrangeEmoji} Orange Won! {Game.BlueEmoji} Blue Won!\r\n{Game.OvertimeEmoji} Went to OT! {Game.EndedEmoji} End Game!");

            return await ReplyAsync($"**New Mafia Game - Mode({game.Mode}), NumMafia({game.Mafia.Count})**", false, embedBuilder.Build());
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
