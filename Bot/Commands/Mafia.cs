using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace TyniBot
{
    public class Mafia : ModuleBase<TyniCommandContext>
    {
        public class MafiaGame
        {
            public IEnumerable<IUser> Team1 = null;
            public IEnumerable<IUser> Team2 = null;
            public IEnumerable<IUser> Mafia = null;
        }

        [Command("mafia"), Summary("Picks the mafia!")]
        public async Task mafiagame(int numMafias, [Remainder]string message = "")
        {
            var errMsg = ValidateCommandInputs(Context.Message.MentionedUsers, numMafias);

            var game = CreateGame(Context.Message.MentionedUsers, numMafias);

            await NotifyStartOfGame(game);
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

            var game = new MafiaGame();

            game.Mafia = mentions.Shuffle().ToList().Take(numMafias); // shuffle again and pick mafia

            // separate teams
            game.Team1 = shuffled.Take(team1Size);
            game.Team2 = shuffled.Skip(team1Size);

            return game;
        }

        public async Task NotifyStartOfGame(MafiaGame game)
        {
            // Send messages to mafia
            foreach (var user in game.Mafia)
            {
                await user.SendMessageAsync("You are in the Mafia!");
            }

            // Send messages to Team 1
            string msg = "Team1: ";
            foreach (var user in game.Team1)
            {
                await user.SendMessageAsync("You are to fight on Team 1!");
                msg += $"{user.Mention} ";
            }

            // Send messages to team 2
            msg += "Team2: ";
            foreach (var user in game.Team2)
            {
                await user.SendMessageAsync("You are to fight on Team 2!");
                msg += $"{user.Mention} ";
            }

            // Send message to channel specifying teams
            await Context.Channel.SendMessageAsync(msg);
        }
    }
}
