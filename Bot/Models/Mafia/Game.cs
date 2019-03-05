using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TyniBot.Mafia
{
    public enum GameMode
    {
        Normal,
        Joker,
        Battle
    }

    public class Game
    {
        [BsonId]
        public ulong Id { get; set; }
        public Dictionary<ulong, Player> Players { get; private set; }
        public Dictionary<ulong, ulong[]> Votes { get; set; } = new Dictionary<ulong, ulong[]>();
        public GameMode Mode { get; private set; }

        [BsonIgnore]
        public List<Player> Team1 => Players.Where(p => p.Value.Team == Team.One).Select(p => p.Value).ToList();
        [BsonIgnore]
        public List<Player> Team2 => Players.Where(p => p.Value.Team == Team.Two).Select(p => p.Value).ToList();
        [BsonIgnore]
        public List<Player> Villagers => Players.Where(p => p.Value.Type == PlayerType.Villager).Select(p => p.Value).ToList();
        [BsonIgnore]
        public List<Player> Mafia => Players.Where(p => p.Value.Type == PlayerType.Mafia).Select(p => p.Value).ToList();
        [BsonIgnore]
        public Player Joker => Players.Where(p => p.Value.Type == PlayerType.Joker).Select(p => p.Value).FirstOrDefault();

        public static Game CreateGame(List<IUser> mentions, int numMafias, GameMode mode = GameMode.Normal)
        {
            if (mentions == null)
                throw new ArgumentNullException(nameof(mentions));

            if (mentions.Where(u => u.IsBot || u.IsWebhook).Count() > 0)
                throw new Exception("Players mentioned must not be Bots or Webhooks you hacker!");

            // Validate that we have more than zero mafia
            if (numMafias <= 0)
                throw new Exception("Number must be positive dipstick!");

            // Validate that more than one users were mentioned
            if (mentions == null || mentions.Count <= 1)
                throw new Exception("You need more than 1 person to play! Mention some friends! You have friends don't you?");

            // Validate that the number of joker + mafia is not greater than number of players
            if(mode == GameMode.Joker && numMafias + 1 > mentions.Count)
                throw new Exception("Number of mafia plus joker can't exceed number of players einstein!");

            // Validate that number of mafia is less than number of players
            if (numMafias >= mentions.Count)
                throw new Exception("Number of mafia can not be equal or exceed players moron!");

            switch (mode)
            {
                case GameMode.Normal:
                default:
                    return createNormalMafiaGame(mentions, numMafias);
                case GameMode.Battle:
                    return createBattleMafiaGame(mentions, numMafias, hasJoker: false);
                case GameMode.Joker:            
                    return createBattleMafiaGame(mentions, numMafias, hasJoker: true);
            };
        }

        public void Vote(ulong userId, List<ulong> mafias)
        {
            if (mafias.Count <= 0)
                return;

            if (Votes == null)
                Votes = new Dictionary<ulong, ulong[]>();

            var users = Team1.Concat(Team2).ToDictionary(x => x.Id);
            if (!users.ContainsKey(userId)) return; // filter out people voting who aren't in the game
                
            Votes[userId] = mafias
                .Where(x => users.ContainsKey(x))   // filter out votes for users not in the game
                .Take(Mafia.Count)                  // only accept the first votes of up to the number of mafia
                .ToArray();
        }

        public Dictionary<ulong, int> Score(int team1Score, int team2Score, string overtime = "no")
        {
            bool hitOvertime = false;
            overtime = overtime.ToLower();
            if(overtime == "overtime" || overtime == "ot" || overtime == "true" || overtime == "yes")
            {
                hitOvertime = true;
            }

            var scores = new Dictionary<ulong, int>();
            foreach (var player in Players.Values)
            {
                int score = 0;
                bool wonGame = (player.Team == Team.One && team1Score > team2Score) || (player.Team == Team.Two && team2Score > team1Score);

                if (player.Type == PlayerType.Mafia)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += !wonGame ? ScoringConstants.LosingAsMafia : 0;
                    score += ScoringConstants.MafiaNobodyGuessedMe - guessedMe;  // two points minus number of guesses as mafia
                }
                else if(player.Type == PlayerType.Joker)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += hitOvertime ? ScoringConstants.ReachedOvertime : 0;
                    score += Math.Min(ScoringConstants.JokerGuessedAsMafiaMax, guessedMe);
                }
                else
                {
                    int correctVotes = Votes.ContainsKey(player.Id) ? Mafia.Where(x => Votes[player.Id].Contains(x.Id)).Count() : 0;

                    score += wonGame ? ScoringConstants.WinningGame : 0;
                    score += correctVotes * ScoringConstants.GuessedMafia;
                }

                scores.Add(player.Id, Math.Max(0,score));
            }

            return scores;
        }

        public void PopulateUser(Func<ulong, IUser> getUser)
        {
            foreach (var u in Players.Values)
                u.DiscordUser = getUser(u.Id);
        }

        private static Game createNormalMafiaGame(List<IUser> users, int numMafias)
        {
            var players = divideTeams(users);

            pickMafia(players, numMafias, divideEvenly: false);

            Dictionary<ulong, Player> gamePlayers = null;
            try
            {
                gamePlayers = players.ToDictionary(u => u.Id);
            }
            catch (ArgumentException e)
            {
                throw new Exception("Each player must be unique dufus!", e);
            }
            return new Game()
            {
                Mode = GameMode.Normal,
                Players = gamePlayers
            };
        }

        private static Game createBattleMafiaGame(List<IUser> users, int numMafias, bool hasJoker = false)
        {
            var players = divideTeams(users);

            pickMafia(players, numMafias, divideEvenly: true);

            if (hasJoker)
                pickJoker(players);

            Dictionary<ulong, Player> gamePlayers = null;
            try
            {
                gamePlayers = players.ToDictionary(u => u.Id);
            }
            catch(ArgumentException e)
            {
                throw new Exception("Each player must be unique dufus!", e);
            }

            return new Game()
            {
                Mode = hasJoker ? GameMode.Joker : GameMode.Battle,
                Players =  gamePlayers
            };
        }

        private static List<Player> divideTeams(List<IUser> users)
        {
            // shuffle teams we call ToList after shuffle to solidfy the list and ensure select doesn't occur randomly
            var players = users.Shuffle().ToList().Select(u => new Player() { Id = u.Id, Type = PlayerType.Villager, DiscordUser = u }).ToList();

            Random rnd = new Random(); // randomize team sizes
            int team1Size = rnd.Next(2) % 2 == 0 ? users.Count / 2 : users.Count - (users.Count / 2);

            foreach (var p in players.Take(team1Size))
                p.Team = Team.One;

            foreach (var p in players.Skip(team1Size))
                p.Team = Team.Two;

            return players;
        }

        private static void pickMafia(List<Player> players, int numMafias, bool divideEvenly = false)
        {
            if (!divideEvenly)
            {
                var mafia = players.Shuffle().ToList().Take(numMafias);
                foreach (var p in mafia)
                    p.Type = PlayerType.Mafia;
            }
            else
            {
                int team1Size = players.Where(p => p.Team == Team.One).Count();
                int team2Size = players.Where(p => p.Team == Team.Two).Count();

                int smallMafiaTeam = numMafias / 2;
                int largeMafiaTeam = numMafias - smallMafiaTeam;

                bool team1LargerMafia = false;
                if (team1Size == team2Size)
                {
                    if (numMafias % 2 == 1) // odd # of Mafia + even teams == randomize Mafia inbalance
                    {
                        Random rnd = new Random();
                        team1LargerMafia = rnd.Next(2) % 2 == 0 ? true : false;
                    }
                }
                else if(team1Size > team2Size)
                {
                    team1LargerMafia = true;
                }

                int team1MafiaSize = team1LargerMafia ? largeMafiaTeam : smallMafiaTeam;
                int team2MafiaSize = team1LargerMafia ? smallMafiaTeam : largeMafiaTeam;

                foreach (var mafia in players.Where(p => p.Team == Team.One).Take(team1MafiaSize))
                    mafia.Type = PlayerType.Mafia;

                foreach (var mafia in players.Where(p => p.Team == Team.Two).Take(team2MafiaSize))
                    mafia.Type = PlayerType.Mafia;
            }
        }

        private static void pickJoker(List<Player> players)
        {
            var team1 = players.Where(p => p.Team == Team.One && p.Type == PlayerType.Villager).ToList();
            var team2 = players.Where(p => p.Team == Team.Two && p.Type == PlayerType.Villager).ToList();

            Player joker = team1.Count > team2.Count ? team1.Where(p => p.Type == PlayerType.Villager).First() : team2.Where(p => p.Type == PlayerType.Villager).First();
            joker.Type = PlayerType.Joker;
        }
    }
}
