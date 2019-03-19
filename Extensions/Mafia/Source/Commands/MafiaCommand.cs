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
            await Output.HelpText(Context.Channel);
        }
        #endregion

        #region Helpers
        private async Task CreateGame(int numMafias, string gameMode = "default")
        {
            GameMode mode = GameMode.Normal;
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
                game = Game.CreateGame(Context.Message.MentionedUsers.Select(s => (IUser)s).ToList(), numMafias, mode);

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
                var existingGame = await Game.GetGameAsync(Context.Channel.Id, Context.Client, games);
                if (existingGame != null)
                    games.Delete(g => g.Id == existingGame.Id);
            }
            catch(Exception){}

            // Insert into DB
            games.Insert(game);
            games.EnsureIndex(x => x.Id);

            IUserMessage scoringMessage = await Output.StartGame(game, Context.Channel);

            // Register scoring message for reaction handler callback
            var reactionHandlers = Context.Database.GetCollection<IReactionHandler>();
            reactionHandlers.Insert(new GameHandler() { MsgId = scoringMessage.Id, GameId = game.Id });
            reactionHandlers.EnsureIndex(x => x.MsgId);
        }
        #endregion
    }
}
