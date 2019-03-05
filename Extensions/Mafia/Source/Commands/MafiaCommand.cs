using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LiteDB;
using TyniBot;

namespace Discord.Mafia
{
    public class MafiaCommand : ModuleBase<TyniBot.CommandContext>
    {
        #region Commands
        [Command("mafia"), Summary("**!mafia <?gameMode=default(battle|joker|default)> <?numOfMafias=1> <@player1> <@player2>** Creates a game of Mafia!")]
        public async Task NewGameCommand(int numMafias, string gameMode, [Remainder]string message = "") // matches | new 2 @Mentions | new 2 j @Mentions
        {
            await CreateGame(numMafias, gameMode);

            var col = Context.Database.GetCollection<IReactionHandler>();
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

            var collection = Context.Database.GetCollection<Game>();

            // Delete current game if exists
            try
            {
                var existingGame = GetGame(Context.Channel.Id, Context.Guild.GetUser, collection);
                if (existingGame != null)
                    collection.Delete(g => g.Id == existingGame.Id);
            }
            catch { }

            // Insert into DB
            collection.Insert(game);
            collection.EnsureIndex(x => x.Id);

            // Notify each Villager
            foreach (var user in game.Villagers)
                await user.SendMessageAsync("You are a Villager!");

            // Notify each Mafia
            foreach (var user in game.Mafia)
                await user.SendMessageAsync("You are in the Mafia!");

            // Notify each Joker
            if (game.Joker != null)
                await game.Joker.SendMessageAsync("You are the Joker!");

            await OutputGameStart(game);
        }

        private async Task OutputGameEnd(Game game, Dictionary<ulong, int> scores)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = scores.OrderByDescending(x => x.Value);

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            embedBuilder.AddField("Mafia: ", string.Join(' ', game.Mafia.Select(u => u.Mention)));
            if(game.Joker != null)
                embedBuilder.AddField("Joker: ", game.Joker.Mention);

            AddVoteFieldToBuilder(game, embedBuilder);

            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputGameStart(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            await ReplyAsync($"**New Mafia Game - Mode({game.Mode}), NumMafia({game.Mafia.Count})**", false, embedBuilder.Build());

            // Todo: Output Voting reaction message
            //col.Insert(new MafiaReactionHandler() { MsgId = Context.Message.Id }); // register message for our reaction handler

            // Todo: Output Scoring reaction message
        }

        private async Task OutputVotes(Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            AddVoteFieldToBuilder(game, embedBuilder);

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private void AddVoteFieldToBuilder(Game game, EmbedBuilder embedBuilder)
        {
            if (game.Votes.Count > 0)
            {
                var votes = new Dictionary<ulong, int>();
                foreach (var playerVotes in game.Votes)
                {
                    foreach (var vote in playerVotes.Value)
                    {
                        if (votes.ContainsKey(vote))
                            votes[vote] += 1;
                        else
                            votes[vote] = 1;
                    }
                }

                embedBuilder.AddField("Mafia Votes: ", string.Join("\r\n", votes.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));
            }
            else
            {
                embedBuilder.AddField("Mafia Votes: ", "None");
            }
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
