using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TyniBot.Models
{
    public class MafiaGame
    {
        public class CreateGameResult
        {
            public MafiaGame Game = null;
            public string ErrorMsg = null;
        }

        public ulong Id { get; set; }
        public ulong[] Team1Ids { get; set; }
        public ulong[] Team2Ids { get; set; }
        public ulong[] MafiaIds { get; set; }
        public Dictionary<ulong, ulong[]> Votes { get; set; }

        public List<IUser> Team1 = new List<IUser>();
        public List<IUser> Team2 = new List<IUser>();
        public List<IUser> Mafia = new List<IUser>();

        private Dictionary<ulong, IUser> users = null;
        public Dictionary<ulong, IUser> Users()
        {
            if (users == null)
                users = Team1.Concat(Team2).Select(x => x).ToDictionary(x => x.Id);
            return users;
        }

        public static CreateGameResult CreateGame(List<IUser> mentions, int numMafias)
        {

            // Validate that we have more than zero mafia
            if (numMafias <= 0)
                return new CreateGameResult() { ErrorMsg = "Number must be positive dipstick!" };

            // Validate that more than one users were mentioned
            if (mentions == null || mentions.Count <= 1)
                return new CreateGameResult() { ErrorMsg = "You need more than 1 person to play! Mention some friends! You have friends don't you?" };

            // validate that number of mafia is less than number of players
            if (numMafias >= mentions.Count)
                return new CreateGameResult() { ErrorMsg = "Number of mafia can not exceed players moron!" };

            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd

            var game = new MafiaGame()
            {
                Mafia = mentions.Shuffle().ToList().Take(numMafias).ToList(), // shuffle again and pick mafia
                Team1 = shuffled.Take(team1Size).ToList(),
                Team2 = shuffled.Skip(team1Size).ToList(),
            };

            game.MafiaIds = game.Mafia.Select(u => u.Id).ToArray();
            game.Team1Ids = game.Team1.Select(u => u.Id).ToArray();
            game.Team2Ids = game.Team2.Select(u => u.Id).ToArray();

            return new CreateGameResult() { Game = game };
        }

        public void Vote(ulong userId, IEnumerable<ulong> mafias)
        {
            if (Votes == null)
                Votes = new Dictionary<ulong, ulong[]>();

            var users = Team1.Concat(Team2).ToDictionary(x => x.Id);
            if (!users.ContainsKey(userId)) return; // filter out people voting who aren't in the game
                
            Votes[userId] = mafias
                .Where(x => users.ContainsKey(x))   // filter out votes for users not in the game
                .Take(Mafia.Count)                  // only accept the first votes of up to the number of mafia
                .ToArray();
        }

        public Dictionary<ulong, int> Score(int team1Score, int team2Score)
        {
            var scores = new Dictionary<ulong, int>();
            foreach (var user in Users())
            {
                int score = 0;
                bool isMafia = Mafia.Where(x => x.Id == user.Key).Count() > 0;
                bool isTeam1 = Team1.Where(x => x.Id == user.Key).Count() > 0;
                bool isTeam2 = Team2.Where(x => x.Id == user.Key).Count() > 0;
                bool wonGame = (isTeam1 && team1Score > team2Score) || (isTeam2 && team2Score > team1Score);

                if (isMafia)
                {
                    int guessedMe = Votes.Where(x => x.Key != user.Key && x.Value.Contains(user.Key)).Count();

                    score += !wonGame ? 3 : 0; // three points for losing
                    score += 2 - guessedMe;    // two points - the number of people that guessed me
                }
                else
                {
                    int correctVotes = Votes.ContainsKey(user.Key) ? Mafia.Where(x => Votes[user.Key].Contains(x.Id)).Count() : 0;

                    score += wonGame ? 1 : 0;  // one point for winning
                    score += correctVotes * 2; // two points for each correct vote
                }

                scores.Add(user.Key, Math.Max(0, score)); // Players score can't go below zero
            }

            return scores;
        }
    }
}
