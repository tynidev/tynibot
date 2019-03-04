using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using LiteDB;
using Discord.WebSocket;
using Discord.Rest;
using TyniBot.Models;

namespace TyniBot
{
    [Group("mafia")]
    public class MafiaCommand : ModuleBase<TyniCommandContext>
    {
        #region Commands
        [Command("new"), Summary("**!mafia new <num of mafias> <@player1> <@player2>** | Creates a new game!")]
        public async Task NewGameCommand(int numMafias, [Remainder]string message = "")
        {
            var result = MafiaGame.CreateGame(Context.Message.MentionedUsers.Select(s => (IUser)s).ToList(), numMafias);
            if(result.Game == null)
            {
                await Context.Channel.SendMessageAsync(result.ErrorMsg);
                return;
            }
            var game = result.Game;

            var collection = Context.Database.GetCollection<MafiaGame>();

            // Prepare for DB
            game.Id = Context.Channel.Id;

            // Delete current game if exists
            MafiaGame existingGame = null;
            try
            {
                existingGame = GetGame(Context.Channel.Id, Context.Guild.GetUser, collection);
            }
            catch { }

            int removed = 0;
            if (existingGame != null)
                removed = collection.Delete(g => g.Id == existingGame.Id);

            // Insert into DB
            collection.Insert(game);
            collection.EnsureIndex(x => x.Id);

            // Notify each Villager
            foreach (var user in game.getVillagers())
                await user.SendMessageAsync("You are a Villager!");

            // Notify each Mafia user
            foreach (var user in game.Mafia)
                await user.SendMessageAsync("You are in the Mafia!");

            await OutputGameStart(game);
        }

        [Command("setup"), Summary("**!mafia setup <alt game> <num of mafias> <@player1> <@player2>** Creates an alternative game of Mafia!")]
        public async Task NewGameCommand(int numMafias, string gameMode, [Remainder]string message = "")
        {
            var result = MafiaGame.CreateGame(Context.Message.MentionedUsers.Select(s => (IUser)s).ToList(), numMafias, gameMode);
            if (result == null)
            {
                await Context.Channel.SendMessageAsync(result.ErrorMsg);
                return;
            }
            var game = result.Game;

            var collection = Context.Database.GetCollection<MafiaGame>();

            // Prepare for DB
            game.Id = Context.Channel.Id;

            // Delete current game if exists
            MafiaGame existingGame = null;
            try
            {
                existingGame = GetGame(Context.Channel.Id, Context.Guild.GetUser, collection);
            }
            catch { }

            int removed = 0;
            if (existingGame != null)
                removed = collection.Delete(g => g.Id == existingGame.Id);

            // Insert into DB
            collection.Insert(game);
            collection.EnsureIndex(x => x.Id);

            // Notify each Villager
            foreach (var user in game.getVillagers())
                await user.SendMessageAsync("You are a Villager!");

            // Notify each Mafia
            foreach (var user in game.Mafia)
                await user.SendMessageAsync("You are in the Mafia!");

            // Notify each Joker
            foreach (var user in game.Joker)
                await user.SendMessageAsync("You are the Joker!");

            await OutputGameStart(game);
        }

        [Command("vote"), Summary("**!mafia vote <@mafia1> <@mafia2>** | Records who you voted as the Mafia.")]
        public async Task VoteGameCommand([Remainder]string message = "")
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<MafiaGame>());
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find a Mafia game in this channel.");
                return;
            }

            game.Vote(Context.User.Id, Context.Message.MentionedUsers.Select(s => s.Id));

            var collection = Context.Database.GetCollection<MafiaGame>();
            collection.Update(game);

            await OutputVotes(game);
        }

        [Command("score"), Summary("**!mafia score <team1 score> <team2 score> <OverTime?>** | Displays who is what and each player's points. ")]
        public async Task ScoreGameCommand(int team1Score, int team2Score, [Remainder]string overtime = "")
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<MafiaGame>());
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
                var game = GetGame(Context.Channel.Id, Context.Guild.GetUser, Context.Database.GetCollection<MafiaGame>());
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
                      .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
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
        private async Task OutputGameEnd(MafiaGame game, Dictionary<ulong, int> scores)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = scores.OrderByDescending(x => x.Value);

            embedBuilder.AddField("Mafia: ", string.Join(' ', game.Mafia.Select(u => u.Mention)));
            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(o => $"{game.Players[o.Key].Mention} = {o.Value}")));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputGameStart(MafiaGame game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            await ReplyAsync($"**Mafia Game: **", false, embedBuilder.Build());
        }

        private async Task OutputVotes(MafiaGame game)
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

        public static MafiaGame GetGame(ulong id, Func<ulong, IUser> GetUser, LiteCollection<MafiaGame> collection)
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
