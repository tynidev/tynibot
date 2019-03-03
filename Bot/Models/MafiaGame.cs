using Discord;
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

        public int Id { get; set; }
        public ulong MessageId { get; set; }
        public ulong[] Team1Ids { get; set; }
        public ulong[] Team2Ids { get; set; }
        public ulong[] MafiaIds { get; set; }
        public Dictionary<ulong, ulong[]> Votes { get; set; }

        public List<IUser> Team1 = new List<IUser>();
        public List<IUser> Team2 = new List<IUser>();
        public List<IUser> Mafia = new List<IUser>();
        public List<IUser> Joker = new List<IUser>();

        private Dictionary<ulong, IUser> users = null;
        public Dictionary<ulong, IUser> Users()
        {
            if (users == null)
                users = Team1.Concat(Team2).Select(x => x).ToDictionary(x => x.Id);
            return users;
        }

        public static CreateGameResult CreateGame(List<IUser> mentions, int numMafias, string mode = "")
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

            MafiaGame game;

            switch(mode.ToLower())
            {
                case "b":
                case "battle":
                    game = createBattleMafiaGame(mentions, numMafias);
                    break;
                case "j":
                case "joker":
                    game = createJokerMafiaGame(mentions, numMafias);
                    break;
                case "jokerbattle":
                case "jb":
                case "bj":
                    game = createBattleJokerMafiaGame(mentions, numMafias);
                    break;
                default:
                    game = createNormalMafiaGame(mentions, numMafias);
                    break;
            }

            return new CreateGameResult() { Game = game };
        }

        public void Vote(ulong userId, List<IUser> mentionedUsers)
        {
            if (Votes == null)
                Votes = new Dictionary<ulong, ulong[]>();

            var users = Team1.Concat(Team2).ToDictionary(x => x.Id);
            if (!users.ContainsKey(userId)) return; // filter out people voting who aren't in the game

            Votes[userId] = mentionedUsers
                .Where(x => users.ContainsKey(x.Id)) // filter out votes for users not in the game
                .Select(x => x.Id)
                .ToArray();
        }

        public Dictionary<ulong, int> ScoreGame(int team1Score, int team2Score)
        {
            var scores = new Dictionary<ulong, int>();
            foreach (var user in Users())
            {
                int score = 0;
                if (Votes.ContainsKey(user.Key))
                {
                    bool isMafia = Mafia.Where(x => x.Id == user.Key).Count() > 0;
                    bool isTeam1 = Team1.Where(x => x.Id == user.Key).Count() > 0;
                    bool isTeam2 = Team2.Where(x => x.Id == user.Key).Count() > 0;
                    bool isJoker = Joker.Where(x => x.Id == user.Key).Count() > 0;
                    bool wonGame = (isTeam1 && team1Score > team2Score) || (isTeam2 && team2Score > team1Score);

                    if (isMafia)
                    {
                        bool guessedMe = Votes.Where(x => x.Value.Contains(user.Key)).Count() > 0;

                        score += !wonGame ? 2 : 0; // two points for losing
                        score += !guessedMe ? 3 : 0;
                    }
                    else if(isJoker)
                    {
                        score += wonGame ? 1 : 0; // one point for winning

                        // How many votes did they get right?
                        int correctVotes = Mafia.Where(x => Votes[user.Key].Contains(x.Id)).Count();
                        score = correctVotes;
                    }
                    else
                    {
                        score += wonGame ? 1 : 0; // one point for winning

                        // How many votes did they get right?
                        int correctVotes = Mafia.Where(x => Votes[user.Key].Contains(x.Id)).Count();
                        score = correctVotes;
                    }
                }

                scores.Add(user.Key, score);
            }

            return scores;
        }

        private static MafiaGame createNormalMafiaGame(List<IUser> mentions, int numMafias)
        {
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

            return game;
        }

        private static MafiaGame createBattleMafiaGame(List<IUser> mentions, int numMafias)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd
            var team1 = shuffled.Take(team1Size).ToList();
            var team1MafiaSize = numMafias / 2; // round down if odd as well
            var team1Mafia = team1.Shuffle().ToList().Take(team1MafiaSize).ToList();
            var team2 = shuffled.Skip(team1Size).ToList();
            var team2Mafia = team2.Shuffle().ToList().Take(numMafias - team1MafiaSize).ToList();

            var game = new MafiaGame()
            {
                Mafia = team1Mafia.Concat(team2Mafia).Shuffle().ToList(), // keep randomness for publishing publically
                Team1 = team1,
                Team2 = team2
            };

            game.MafiaIds = game.Mafia.Select(u => u.Id).ToArray();
            game.Team1Ids = game.Team1.Select(u => u.Id).ToArray();
            game.Team2Ids = game.Team2.Select(u => u.Id).ToArray();

            return game;
        }

        private static MafiaGame createBattleJokerMafiaGame(List<IUser> mentions, int numMafias)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd
            var team1 = shuffled.Take(team1Size).ToList();
            var team1MafiaSize = numMafias / 2; // round down if odd as well
            var team1Mafia = team1.Shuffle().ToList().Take(team1MafiaSize).ToList();
            var team2 = shuffled.Skip(team1Size).ToList(); // this means team2 will have more people (if odd)
            var team2Mafia = team2.Shuffle().ToList().Take(numMafias - team1MafiaSize).ToList();
            var team2Joker = team2.Where(x => !team2Mafia.Contains(x)).Shuffle().ToList().Take(1).ToList();

            var game = new MafiaGame()
            {
                Mafia = team1Mafia.Concat(team2Mafia).Shuffle().ToList(), // keep randomness for publishing publically
                Team1 = team1,
                Team2 = team2,
                Joker = team2Joker
            };

            game.MafiaIds = game.Mafia.Select(u => u.Id).ToArray();
            game.Team1Ids = game.Team1.Select(u => u.Id).ToArray();
            game.Team2Ids = game.Team2.Select(u => u.Id).ToArray();

            return game;
        }

        private static MafiaGame createJokerMafiaGame(List<IUser> mentions, int numMafias)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd
            var mafia = mentions.Shuffle().ToList().Take(numMafias).ToList();// shuffle again and pick mafia

            var game = new MafiaGame()
            {
                Mafia = mafia, 
                Team1 = shuffled.Take(team1Size).ToList(),
                Team2 = shuffled.Skip(team1Size).ToList(),
                Joker = shuffled.Where(x => !mafia.Contains(x)).Shuffle().ToList().Take(1).ToList()
        };

            game.MafiaIds = game.Mafia.Select(u => u.Id).ToArray();
            game.Team1Ids = game.Team1.Select(u => u.Id).ToArray();
            game.Team2Ids = game.Team2.Select(u => u.Id).ToArray();

            return game;
        }
    }
}
