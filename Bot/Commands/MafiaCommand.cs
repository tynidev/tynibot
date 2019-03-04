using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using LiteDB;

namespace TyniBot
{
    [Group("mafia")]
    public class MafiaCommand : ModuleBase<TyniCommandContext>
    {
        #region Commands
        [Command("new"), Summary("**!mafia new <?gameMode=default(battle|joker|default)> <?numOfMafias=1> <@player1> <@player2>** Creates a game of Mafia!")]
        public async Task NewGameCommand(int numMafias, [Remainder]string message = "")
        {
            await CreateGame(numMafias, "default");
        }
        
        [Command("new")]
        public async Task NewGameCommand2(string gameMode, int numMafias, [Remainder]string message = "")
        {
            await CreateGame(numMafias, gameMode);
        }

        [Command("new")]
        public async Task NewGameCommand3([Remainder]string message = "")
        {
            await CreateGame(1, "default");
        }

        [Command("vote"), Summary("**!mafia vote <@mafia1> <@mafia2>** | Records who you voted as the Mafia.")]
        public async Task VoteGameCommand([Remainder]string message = "")
        {
            Mafia.Game game = null;
            try
            {
                game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<Mafia.Game>());
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find a Mafia game in this channel.");
                return;
            }

            game.Vote(Context.User.Id, Context.Message.MentionedUsers.Select(s => s.Id));

            var collection = Context.Database.GetCollection<Mafia.Game>();
            collection.Update(game);

            await OutputVotes(game);
        }

        [Command("score"), Summary("**!mafia score <team1 score> <team2 score> <?OverTime=no(yes|no)>** | Displays who is what and each player's points. ")]
        public async Task ScoreGameCommand(int team1Score, int team2Score, [Remainder]string overtime = "no")
        {
            Mafia.Game game = null;
            try
            {
                game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<Mafia.Game>());
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find a Mafia game in this channel.");
                return;
            }

            var scores = game.Score(team1Score, team2Score, overtime);

            await OutputGameEnd(game, scores);
        }

        [Command("get"), Summary("**!mafia get** | Displays the game start summary message.")]
        public async Task GetGameCommand()
        {
            try
            {
                var game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<Mafia.Game>());
                await OutputGameStart(game);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find a Mafia game in this channel.");
            }
        }

        [Command("help"), Summary("**!mafia help** | Displays this help text.")]
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
            Mafia.Game game;
            try
            {
                game = Mafia.Game.CreateGame(Context.Message.MentionedUsers.Select(s => (IUser)s).ToList(), numMafias, gameMode);

                // Set game id to ChannelId
                game.Id = Context.Channel.Id;
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            var collection = Context.Database.GetCollection<Mafia.Game>();

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

        private async Task OutputGameEnd(Mafia.Game game, Dictionary<ulong, int> scores)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = scores.OrderByDescending(x => x.Value);

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            embedBuilder.AddField("Mafia: ", string.Join(' ', game.Mafia.Select(u => u.Mention)));
            if(game.Joker != null)
                embedBuilder.AddField("Joker: ", game.Joker.Mention);

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

            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputGameStart(Mafia.Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputVotes(Mafia.Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var votes = new Dictionary<ulong, int>();
            foreach(var playerVotes in game.Votes)
            {
                foreach(var vote in playerVotes.Value)
                {
                    if (votes.ContainsKey(vote))
                        votes[vote] += 1;
                    else
                        votes[vote] = 1;
                }
            }

            embedBuilder.AddField("Mafia Votes: ", string.Join("\r\n", votes.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        public static Mafia.Game GetGame(ulong id, Func<ulong, IUser> GetUser, LiteCollection<Mafia.Game> collection)
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
