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
    public class Mafia : ModuleBase<TyniCommandContext>
    {
        #region Commands
        [Command("new"), Summary("Creates a new game of Mafia!")]
        public async Task NewGameCommand(int numMafias, [Remainder]string message = "")
        {
            var result = MafiaGame.CreateGame(Context.Message.MentionedUsers, numMafias);
            if(result == null)
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

            await NotifyStartOfGame(game);
        }

        [Command("get"), Summary("Gets a stored game of Mafia!")]
        public async Task GetGameCommand(int id)
        {
            try
            {
                var game = GetGame(id, Context.Guild.GetUser);
                await Context.Channel.SendMessageAsync(game.ToString());
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
            }
        }

        [Command("vote"), Summary("Cast your vote for who is the Mafia!")]
        public async Task VoteCommand(int id, [Remainder]string message = "")
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(id, Context.Guild.GetUser);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
                return;
            }

            game.Vote(Context.User.Id, Context.Message.MentionedUsers);

            var collection = Context.Database.GetCollection<MafiaGame>();
            collection.Update(game);
        }

        [Command("score"), Summary("Reveal the Mafia and calculate the Score!")]
        public async Task Score(int id, int team1Score, int team2Score)
        {
            MafiaGame game = null;
            try
            {
                game = GetGame(id, Context.Guild.GetUser);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
                return;
            }

            await Context.Channel.SendMessageAsync(GetScoreAnnouncement(game, team1Score, team2Score));
        }
        #endregion

        #region Helpers
        private string GetScoreAnnouncement(MafiaGame game, int team1Score, int team2Score)
        {
            var ordered = game.ScoreGame(team1Score, team2Score).OrderByDescending(x => x.Value);

            string output = $"**Mafia Game: {game.Id}**\r\n    Mafia: ";
            foreach (var mafia in game.Mafia)
                output += $"{mafia.Mention }";

            foreach (var score in ordered)
                output += $"\r\n    {game.Users[score.Key].Mention} = {score.Value}";

            return output;
        }

        private string GetGameAnnouncement(MafiaGame game)
        {
            // Send messages to Team 1
            string output = $"**Mafia Game: {game.Id}**\r\n\r\n    Team1: ";
            foreach (var user in game.Team1)
            {
                output += $"{user.Mention} ";
            }

            // Send messages to team 2
            output += "\r\n    Team2: ";
            foreach (var user in game.Team2)
            {
                output += $"{user.Mention} ";
            }

            return output;
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

        public async Task NotifyStartOfGame(MafiaGame game)
        {
            // Send messages to mafia
            foreach (var user in game.Mafia)
            {
                await user.SendMessageAsync("You are in the Mafia!");
            }

            // Send message to channel specifying teams
            await Context.Channel.SendMessageAsync(game.ToString());
        }
        #endregion
    }
}
