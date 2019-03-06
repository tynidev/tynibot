﻿using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Mafia
{
    public enum GameMode
    {
        Normal,
        Joker,
        Battle
    }

    public class Game
    {
        public const string OrangeEmoji = "\ud83d\udd36";
        public const string BlueEmoji = "\ud83d\udd37";
        public const string OvertimeEmoji = "\u23f0";
        public const string EndedEmoji = "\ud83c\udfc1";

        [BsonId]
        public ulong Id { get; set; }
        public Dictionary<ulong, Player> Players { get; private set; } = new Dictionary<ulong, Player>();
        public Dictionary<ulong, ulong[]> Votes { get; set; } = new Dictionary<ulong, ulong[]>();
        public GameMode Mode { get; private set; } = GameMode.Normal;
        public Team? WinningTeam { get; set; } = null;
        public bool OvertimeReached { get; set; } = false;
        public ulong HostId { get; set; }

        [BsonIgnore]
        public List<Player> TeamOrange => Players.Where(p => p.Value.Team == Team.Orange).Select(p => p.Value).ToList();
        [BsonIgnore]
        public List<Player> TeamBlue => Players.Where(p => p.Value.Team == Team.Blue).Select(p => p.Value).ToList();
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
        
        public void AddVotes(ulong userId, List<ulong> votes)
        {
            foreach(var v in votes)
            {
                AddVote(userId, v);
            }
        }

        public void AddVote(ulong userId, ulong mafiaId)
        {
            if (!Players.ContainsKey(userId)) return; // filter out people voting who aren't in the game

            if (!Players.ContainsKey(mafiaId)) return; // filter out votes for users not in the game

            if (!Votes.ContainsKey(userId))
            {
                Votes[userId] = new ulong[] { mafiaId };
                return;
            }

            if (Votes[userId].Length > Mafia.Count) return; // only accept the first votes of up to the number of mafia

            if (Votes[userId].Contains(mafiaId)) return; // we already counted this vote

            Votes[userId] = Votes[userId].Append(mafiaId).ToArray();
        }

        public void RemoveVote(ulong userId, ulong mafiaId)
        {
            if (!Players.ContainsKey(userId)) return; // filter out people voting who aren't in the game

            if (!Players.ContainsKey(mafiaId)) return; // filter out votes for users not in the game

            if (!Votes.ContainsKey(userId)) return; // user hasn't voted return

            if (!Votes[userId].Contains(mafiaId)) return; // we don't have this vote anyways

            Votes[userId] = Votes[userId].Where(u => u != mafiaId).ToArray();
        }

        public bool Score()
        {
            var mafia = Mafia;

            if (!WinningTeam.HasValue) return false; // we only score games that have a winner

            // score only valid if all players have voted for the correct number of mafia
            foreach (var p in Players)
                if (!Votes.ContainsKey(p.Value.Id) || Votes[p.Value.Id].Length != mafia.Count) return false;

            foreach (var player in Players.Values)
            {
                int score = 0;
                bool wonGame = player.Team == WinningTeam.Value;

                if (player.Type == PlayerType.Mafia)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += !wonGame ? ScoringConstants.LosingAsMafia : 0;
                    score += ScoringConstants.MafiaNobodyGuessedMe - guessedMe;  // two points minus number of guesses as mafia
                }
                else if(player.Type == PlayerType.Joker)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += OvertimeReached ? ScoringConstants.ReachedOvertime : 0;
                    score += Math.Min(ScoringConstants.JokerGuessedAsMafiaMax, guessedMe);
                }
                else
                {
                    int correctVotes = Votes.ContainsKey(player.Id) ? mafia.Where(x => Votes[player.Id].Contains(x.Id)).Count() : 0;

                    score += wonGame ? ScoringConstants.WinningGame : 0;
                    score += correctVotes * ScoringConstants.GuessedMafia;
                }

                player.Score = Math.Max(0,score);
            }

            return true;
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
                p.Team = Team.Orange;

            foreach (var p in players.Skip(team1Size))
                p.Team = Team.Blue;

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
                int team1Size = players.Where(p => p.Team == Team.Orange).Count();
                int team2Size = players.Where(p => p.Team == Team.Blue).Count();

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

                foreach (var mafia in players.Where(p => p.Team == Team.Orange).Take(team1MafiaSize))
                    mafia.Type = PlayerType.Mafia;

                foreach (var mafia in players.Where(p => p.Team == Team.Blue).Take(team2MafiaSize))
                    mafia.Type = PlayerType.Mafia;
            }
        }

        private static void pickJoker(List<Player> players)
        {
            var team1 = players.Where(p => p.Team == Team.Orange && p.Type == PlayerType.Villager).ToList();
            var team2 = players.Where(p => p.Team == Team.Blue && p.Type == PlayerType.Villager).ToList();

            Player joker = team1.Count > team2.Count ? team1.Where(p => p.Type == PlayerType.Villager).First() : team2.Where(p => p.Type == PlayerType.Villager).First();
            joker.Type = PlayerType.Joker;
        }
    }
}
