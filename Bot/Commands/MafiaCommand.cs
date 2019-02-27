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
            game.Id = collection.Count() + 1;
            game.MessageId = Context.Message.Id;

            // Insert into DB
            collection.Insert(game);
            collection.EnsureIndex(x => x.Id);

            // Notify each Mafia user
            foreach(var user in game.Mafia)
                await user.SendMessageAsync("You are in the Mafia!");

            await OutputGameStart(game);
        }

        [Command("vote"), Summary("**!mafia vote <game id> <@mafia1> <@mafia2>** | Records who you voted as the Mafia.")]
        public async Task VoteCommand(int id, [Remainder]string message = "")
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(id, Context.Guild.GetUser);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
                return;
            }

            game.Vote(Context.User.Id, Context.Message.MentionedUsers.Select(s => s.Id));

            var collection = Context.Database.GetCollection<MafiaGame>();
            collection.Update(game);
        }

        [Command("score"), Summary("**!mafia score <game id> <team1 score> <team2 score>** | Displays who is in the Mafia and each player's points. ")]
        public async Task Score(int id, int team1Score, int team2Score)
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(id, Context.Guild.GetUser);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
                return;
            }

            var scores = game.Score(team1Score, team2Score);

            await OutputGameEnd(game, scores);
        }

        [Command("get"), Summary("**!mafia get <game id>** | Displays the game start summary message.")]
        public async Task GetGameCommand(int id)
        {
            try
            {
                var game = GetGame(id, Context.Guild.GetUser);
                await OutputGameStart(game);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
            }
        }

        [Command("help"), Summary("**!mafia help** | Displays this help text.")]
        public async Task Help()
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
            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(o => $"{game.Users()[o.Key].Mention} = {o.Value}")));

            await ReplyAsync($"**Mafia Game: {game.Id}**", false, embedBuilder.Build());
        }

        private async Task OutputGameStart(MafiaGame game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Team 1:", string.Join(' ', game.Team1.Select(u => u.Mention)));
            embedBuilder.AddField("Team 2:", string.Join(' ', game.Team2.Select(u => u.Mention)));

            await ReplyAsync($"**Mafia Game: {game.Id}**", false, embedBuilder.Build());
        }

        private MafiaGame GetGame(int id, Func<ulong, IUser> GetUser)
        {
            var collection = Context.Database.GetCollection<MafiaGame>();
            var game = collection.FindById(id);
            game.Mafia = game.MafiaIds.Select(x => GetUser(x)).ToList();
            game.Team1 = game.Team1Ids.Select(x => GetUser(x)).ToList();
            game.Team2 = game.Team2Ids.Select(x => GetUser(x)).ToList();
            return game;
        }
        #endregion
    }
}
