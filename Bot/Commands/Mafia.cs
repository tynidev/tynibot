using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using LiteDB;
using Discord.WebSocket;
using Discord.Rest;

namespace TyniBot
{
    [Group("mafia")]
    public class Mafia : ModuleBase<TyniCommandContext>
    {
        public class MafiaGame
        {
            public int Id { get; set; }
            public ulong MessageId { get; set; }
            public ulong[] Team1Ids { get; set; }
            public ulong[] Team2Ids{ get; set; }

            public List<IUser> Team1 = null;
            public List<IUser> Team2 = null;
            public List<IUser> Mafia = null;

            public override string ToString()
            {
                // Send messages to Team 1
                string msg = $"**Mafia Game: {Id}**\r\n\r\n    Team1: ";
                foreach (var user in Team1)
                {
                    msg += $"{user.Mention} ";
                }

                // Send messages to team 2
                msg += "\r\n    Team2: ";
                foreach (var user in Team2)
                {
                    msg += $"{user.Mention} ";
                }

                return msg;
            }
        }

        [Command("new"), Summary("Creates a new game of Mafia!")]
        public async Task NewGame(int numMafias, [Remainder]string message = "")
        {
            var errMsg = ValidateCommandInputs(Context.Message.MentionedUsers, numMafias);
            if(errMsg != null)
            {
                await Context.Channel.SendMessageAsync(errMsg);
                return;
            }

            var game = CreateGame(Context.Message.MentionedUsers, numMafias);

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
        public async Task GetGame(int id)
        {
            var collection = Context.Database.GetCollection<MafiaGame>();
            try
            {
                var game = collection.FindById(id);
                game.Team1 = game.Team1Ids.Select(x => (IUser)Context.Guild.GetUser(x)).ToList();
                game.Team2 = game.Team2Ids.Select(x => (IUser)Context.Guild.GetUser(x)).ToList();
                await Context.Channel.SendMessageAsync(game.ToString());
            }
            catch(Exception e)
            {
                await Context.Channel.SendMessageAsync($"Could not find Mafia game with Id: {id}");
            }
        }

        public static string ValidateCommandInputs(IReadOnlyCollection<IUser> mentions, int numMafias)
        {
            // Validate that we have more than zero mafia
            if (numMafias <= 0)
                return "Number must be positive dipstick!";

            // Validate that more than one users were mentioned
            if (mentions == null || mentions.Count <= 1)
                return "You need more than 1 person to play! Mention some friends! You have friends don't you?";

            // validate that number of mafia is less than number of players
            if (numMafias >= mentions.Count)
                return "Number of mafia can not exceed players moron!";

            return null;
        }

        public static MafiaGame CreateGame(IReadOnlyCollection<IUser> mentions, int numMafias)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd

            var game = new MafiaGame()
            {
                Mafia = mentions.Shuffle().ToList().Take(numMafias).ToList(), // shuffle again and pick mafia
                Team1 = shuffled.Take(team1Size).ToList(),
                Team2 = shuffled.Skip(team1Size).ToList(),
            };

            game.Team1Ids = game.Team1.Select(u => u.Id).ToArray();
            game.Team2Ids = game.Team2.Select(u => u.Id).ToArray();

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
    }
}
