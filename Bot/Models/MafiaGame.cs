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
                Team1 = shuffled.Take(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = true, OnTeam2 = false, DiscordUser = u}).ToList(),
                Team2 = shuffled.Skip(team1Size).Select(u => new MafiaPlayer() { Id = u.Id, IsMafia = false, OnTeam1 = false, OnTeam2 = true, DiscordUser = u }).ToList(),
                Mafia = new List<MafiaPlayer>(),
            };

            Random rnd = new Random();
            while(numMafias > 0)
            {
                var nonMafia = game.Players.Values.Where(u => !u.IsMafia).ToList();
                var player = nonMafia[rnd.Next(nonMafia.Count)];

                player.IsMafia = true;
                game.Mafia.Add(player);

                numMafias--;
            }

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
            foreach (var kv in Players)
            {
                var player = Players[kv.Key];

                int score = 0;
                bool wonGame = (player.OnTeam1 && team1Score > team2Score) || (player.OnTeam2 && team2Score > team1Score);

                if (player.IsMafia)
                {
                    int guessedMe = Votes.Where(x => x.Key != player.Id && x.Value.Contains(player.Id)).Count();

                    score += !wonGame ? 3 : 0; // three points for losing
                    score += 2 - guessedMe;    // two points - the number of people that guessed me
                }
                else
                {
                    int correctVotes = Votes.ContainsKey(player.Id) ? Mafia.Where(x => Votes[player.Id].Contains(x.Id)).Count() : 0;

                    score += wonGame ? 1 : 0;  // one point for winning
                    score += correctVotes * 2; // two points for each correct vote
                }

                scores.Add(player.Id, Math.Max(0, score)); // Players score can't go below zero
            }

            return scores;
        }

        public void PopulateUser(Func<ulong, IUser> getUser)
        {
            foreach (var u in Mafia)
                u.DiscordUser = getUser(u.Id);
            foreach (var u in Team1)
                u.DiscordUser = getUser(u.Id);
            foreach (var u in Team2)
                u.DiscordUser = getUser(u.Id);
        }
    }
}
