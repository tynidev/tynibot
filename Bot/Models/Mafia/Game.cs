﻿using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TyniBot.Mafia
{
    public class CreateGameResult
    {
        public Game Game = null;
        public string ErrorMsg = null;
    }

    public class Game
    {
        [BsonId]
        public ulong Id { get; set; }
        [BsonId]
        public Dictionary<ulong, Player> Players { get; private set; }

        [BsonIgnore]
        public Dictionary<ulong, ulong[]> Votes { get; set; }
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

        public static Game CreateGame(List<IUser> mentions, int numMafias, string mode = "")
        {
            // Validate that we have more than zero mafia
            if (numMafias <= 0)
                throw new Exception("Number must be positive dipstick!");

            // Validate that more than one users were mentioned
            if (mentions == null || mentions.Count <= 1)
                throw new Exception("You need more than 1 person to play! Mention some friends! You have friends don't you?");

            // validate that number of mafia is less than number of players
            if (numMafias >= mentions.Count)
                throw new Exception("Number of mafia can not be equal or exceed players moron!");

            Game game = null;

            switch (mode.ToLower())
            {
                case "b":
                case "battle":
                    game = createBattleMafiaGame(mentions, numMafias, hasJoker: false);
                    break;
                // for now, joker is an extension of the battle format
                case "j":
                case "joker":
                    game = createBattleMafiaGame(mentions, numMafias, hasJoker: true);
                    break;
                default:
                    game = createNormalMafiaGame(mentions, numMafias);
                    break;
            };

            return game;
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

        public Dictionary<ulong, int> Score(int team1Score, int team2Score, string overtime = "")
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
                    int hiddenScore = ScoringConstants.MaxHiddenAsMafia - guessedMe;

                    score += !wonGame ? ScoringConstants.LosingAsMafia : 0;
                    score += hiddenScore < 0 ? 0 : hiddenScore;  // two points minus number of guesses as mafia
                }
                else if(player.Type == PlayerType.Joker)
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

                scores.Add(player.Id, score);
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

            return new Game()
            {
                Players = players.ToDictionary(u => u.Id)
            };
        }

        private static Game createBattleMafiaGame(List<IUser> users, int numMafias, bool hasJoker = false)
        {
            var players = divideTeams(users);

            pickMafia(players, numMafias, divideEvenly: false);

            if (hasJoker)
                pickJoker(players);

            return new Game()
            {
                Players = players.ToDictionary(u => u.Id)
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
                bool team1Larger = false;
                Random rnd = new Random();
                if (numMafias % 2 == 1 && players.Count % 2 == 0) // odd # of Mafia + even teams == randomize Mafia inbalance
                {
                    team1Larger = rnd.Next(2) % 2 == 0 ? true : false;
                }

                int team1Count = !team1Larger ? numMafias / 2 : numMafias - (numMafias / 2);
                int team2Count = numMafias - team1Count;
                
                foreach (var mafia in players.Where(p => p.Team == Team.One).Take(team1Count))
                    mafia.Type = PlayerType.Mafia;

                foreach (var mafia in players.Where(p => p.Team == Team.Two).Take(team2Count))
                    mafia.Type = PlayerType.Mafia;
            }
        }

        private static void pickJoker(List<Player> players)
        {
            var joker = players.Where(p => p.Type == PlayerType.Villager).First();
            joker.Type = PlayerType.Joker;
        }
    }
}
