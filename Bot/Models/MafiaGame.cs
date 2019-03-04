using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TyniBot.Models
{
    public class CreateGameResult
    {
        public MafiaGame Game = null;
        public string ErrorMsg = null;
    }

    public class MafiaGame
    {
        [BsonId]
        public ulong Id { get; set; }
        public Dictionary<ulong, ulong[]> Votes { get; set; }
        public List<MafiaPlayer> Team1 { get; private set; }
        public List<MafiaPlayer> Team2 { get; private set; }
        public List<MafiaPlayer> Mafia { get; private set; }
        public List<MafiaPlayer> Joker { get; private set; }

        [BsonIgnore]
        public Dictionary<ulong, MafiaPlayer> Players
        {
            get
            {
                if (players == null && (Team1 != null && Team2 != null))
                    players = Team1.Concat(Team2).ToDictionary(x => x.Id);
                return players;
            }
        }
        
        private Dictionary<ulong, MafiaPlayer> players = null;

        public static CreateGameResult CreateGame(List<IUser> mentions, int numMafias, string mode = "")
        {
            // Validate that we have more than zero mafia
            if (numMafias <= 0)
                return new CreateGameResult() { ErrorMsg = "Number must be positive dipstick!" };

            // Validate that more than one users were mentioned
            if (mentions == null || mentions.Count <= 1)
                return new CreateGameResult() { ErrorMsg = "You need more than 1 person to play! Mention some friends! You have friends don't you?" };

            // validate that number of mafia is less than number of players
            if (numMafias + 1 >= mentions.Count)
                return new CreateGameResult() { ErrorMsg = "Number of mafia can not be equal or exceed players moron!" };

            MafiaGame game;

            switch (mode.ToLower())
            {
                case "b":
                case "battle":
                    game = createBattleMafiaGame(mentions, numMafias);
                    break;
                // for now, joker is an extension of the battle format
                case "j":
                case "joker":
                    game = createBattleMafiaGame(mentions, numMafias, true);
                    break;
                default:
                    game = createNormalMafiaGame(mentions, numMafias);
                    break;
            };

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

        public Dictionary<ulong, int> Score(int team1Score, int team2Score, string overtime)
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
                bool wonGame = (player.OnTeam1 && team1Score > team2Score) || (player.OnTeam2 && team2Score > team1Score);

                if (player.IsMafia)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += !wonGame ? ScoringConstants.LosingAsMafia : 0;
                    score += ScoringConstants.MaxHiddenAsMafia - guessedMe;  // two points minus number of guesses as mafia
                }
                else if(player.IsJoker)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += hitOvertime ? ScoringConstants.ReachingOvertime : 0;
                    score += guessedMe > ScoringConstants.MaxMafiaGuessAsJoker ? ScoringConstants.MaxMafiaGuessAsJoker : guessedMe;
                }
                else
                {
                    int correctVotes = Votes.ContainsKey(player.Id) ? Mafia.Where(x => Votes[player.Id].Contains(x.Id)).Count() : 0;

                    score += wonGame ? ScoringConstants.WinningGame : 0;
                    score += correctVotes * ScoringConstants.GuessingMafia;
                }

                scores.Add(player.Id, Math.Max(0, score)); // Players score can't go below zero
            }

            return scores;
        }

        public void PopulateUser(Func<ulong, IUser> getUser)
        {
            foreach (var u in Players.Values)
                u.DiscordUser = getUser(u.Id);
        }

        public List<MafiaPlayer> getVillagers(bool pickOnTeam1)
        {
            if (pickOnTeam1)
            {
                return Team1.Where(u => !u.IsMafia && !u.IsJoker).ToList();
            }
            else
            {
                return Team2.Where(u => !u.IsMafia && !u.IsJoker).ToList();
            }
        }

        public List<MafiaPlayer> getVillagers()
        {
            return Players.Values.Where(u => !u.IsMafia && !u.IsJoker).ToList();
        }

        private static MafiaGame createNormalMafiaGame(List<IUser> mentions, int numMafias)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd

            var game = new MafiaGame()
            {
                Team1 = shuffled.Take(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = true, OnTeam2 = false, DiscordUser = u }).ToList(),
                Team2 = shuffled.Skip(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = false, OnTeam2 = true, DiscordUser = u }).ToList(),
                Mafia = new List<MafiaPlayer>(),
            };

            Random rnd = new Random();
            while (numMafias > 0)
            {
                var villagers = game.getVillagers();
                var player = villagers[rnd.Next(villagers.Count)];

                player.IsMafia = true;
                game.Mafia.Add(player);

                numMafias--;
            }

            return game;
        }

        private void chooseJoker(Random rnd, bool pickTeam1Villagers)
        {
            List<MafiaPlayer> villagers = getVillagers(pickTeam1Villagers);
            MafiaPlayer player = villagers[rnd.Next(villagers.Count)];
            player.IsJoker = true;
            Joker.Add(player);
        }

        private static MafiaGame createBattleMafiaGame(List<IUser> mentions, int numMafias, bool hasJoker = false)
        {
            var shuffled = mentions.Shuffle().ToList(); // shuffle teams we call ToList to solidfy the list
            var team1Size = mentions.Count / 2; // round down if odd, meaning more people on team 2
            
            var game = new MafiaGame()
            {
                Team1 = shuffled.Take(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = true, OnTeam2 = false, DiscordUser = u }).ToList(),
                Team2 = shuffled.Skip(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = false, OnTeam2 = true, DiscordUser = u }).ToList(),
                Mafia = new List<MafiaPlayer>(),
                Joker = new List<MafiaPlayer>()
            };

            bool pickOnTeam1 = false; // start picking mafia with team 2
            List<MafiaPlayer> villagers;
            MafiaPlayer player;
            Random rnd = new Random();

            while (numMafias > 0)
            {
                villagers = game.getVillagers(pickOnTeam1);
                pickOnTeam1 = !pickOnTeam1;
                player = villagers[rnd.Next(villagers.Count)];

                player.IsMafia = true;
                game.Mafia.Add(player);

                numMafias--;
            }

            if(hasJoker)
            {
                game.chooseJoker(rnd, pickOnTeam1);
            }

            return game;
        }
    }
}
