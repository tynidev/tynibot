using Discord;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TyniBot;

namespace UnitTests
{
    [TestClass]
    public class MafiaTests
    {
        [TestMethod]
        public void TestDbStoreRetrieveGame()
        {
            using (var Database = new LiteDatabase(@"test.db"))
            {
                var mentions = new List<IUser>();

                var user1 = new Mock<IUser>();
                user1.Setup(u => u.Username).Returns("bob");
                user1.Setup(u => u.Id).Returns(1);
                mentions.Add(user1.Object);

                var user2 = new Mock<IUser>();
                user2.Setup(u => u.Username).Returns("joe");
                user2.Setup(u => u.Id).Returns(2);
                mentions.Add(user2.Object);

                var input = TyniBot.Mafia.Game.CreateGame(mentions, 1);
                input.Id = 1;

                var gamesCollection = Database.GetCollection<TyniBot.Mafia.Game>();
                gamesCollection.Delete(u => true);
                gamesCollection.Insert(input);
                gamesCollection.EnsureIndex(x => x.Id);

                var output = MafiaCommand.GetGame(input.Id, (ulong id) =>
                {
                    if (id == user1.Object.Id)
                        return user1.Object;
                    else
                        return user2.Object;
                }, gamesCollection);

                Assert.AreEqual(input.Players.Count, output.Players.Count);
                Assert.AreEqual(output.Mafia.Where(u => input.Mafia.Where(o => o.Id != u.Id).Count() > 0).Count(), 0);
                Assert.AreEqual(output.Team1.Where(u => input.Team1.Where(o => o.Id != u.Id).Count() > 0).Count(), 0);
                Assert.AreEqual(output.Team2.Where(u => input.Team2.Where(o => o.Id != u.Id).Count() > 0).Count(), 0);
            }
        }

        [TestMethod]
        public void TestCreateGameGeneratesValidGame()
        {
            Random r = new Random();
                 
            for (int j = 0; j < 100; j++)
            {
                var mentions = new List<IUser>();
                for (int i = 0; i < (j % 7) + 2; i++)
                {
                    var user = new Mock<IUser>();
                    user.Setup(u => u.Id).Returns((ulong)i);
                    user.Setup(u => u.Username).Returns(i.ToString());
                    mentions.Add(user.Object);
                }

                for (int i = 0; i < 300; i++)
                {
                    var numMafia = (i % (mentions.Count - 1)) + 1;
                    int random = r.Next(3);
                    string mode = "";
                    if (random == 0)
                        mode = "default";
                    if (random == 1)
                        mode = "battle";
                    if (random == 2)
                        mode = "joker";

                    var game = (TyniBot.Mafia.Game.CreateGame(mentions, numMafia, mode));

                    Assert.AreEqual(numMafia, game.Mafia.Count()); // validate actual number of mafia was as requested
                    Assert.AreEqual(game.Team1.Count() + game.Team2.Count(), mentions.Count); // validate members of both teams equals total count of mentions

                    if (mode == "joker")
                    {
                        Assert.IsNotNull(game.Joker);
                        Assert.IsTrue(mentions.Contains(game.Joker.DiscordUser));
                    }

                    if(mode == "joker" || mode == "battle")
                    {
                        int team1Mafia = game.Mafia.Where(u => u.Team == TyniBot.Mafia.Team.One).Count();
                        int team2Mafia = game.Mafia.Where(u => u.Team == TyniBot.Mafia.Team.Two).Count();

                        if(numMafia > 1) // assert mafia aren't all on one team
                        {
                            Assert.AreNotEqual(0, team1Mafia);
                            Assert.AreNotEqual(0, team2Mafia);

                            if (numMafia % 2 == 0) // even
                            {
                                Assert.AreEqual(team1Mafia, team2Mafia); // assert evenly split
                            }
                            else // odd
                            {
                                int sub = team1Mafia > team2Mafia ? team1Mafia - team2Mafia : team2Mafia - team1Mafia;
                                Assert.AreEqual(1, sub);
                            }
                        }
                    }

                    var mafia = new Dictionary<string, string>();
                    var t1 = new Dictionary<string, string>();
                    var t2 = new Dictionary<string, string>();

                    foreach (var u in game.Mafia)
                    {
                        Assert.IsTrue(mentions.Contains(u.DiscordUser)); // validate each mafia member was part of original mentions
                        Assert.IsFalse(mafia.ContainsKey(u.Username)); // validate users weren't added to mafia twice
                        mafia.Add(u.Username, u.Username);
                    }
                    foreach (var u in game.Team1)
                    {
                        t1.Add(u.Username, u.Username);
                        Assert.IsTrue(mentions.Contains(u.DiscordUser)); // validate every team member was part of original mentions
                    }
                    foreach (var u in game.Team2)
                    {
                        t2.Add(u.Username, u.Username);
                        Assert.IsTrue(mentions.Contains(u.DiscordUser)); // validate every team member was part of original mentions
                        Assert.IsFalse(t1.ContainsKey(u.Username)); // validate every team2 member is not in team 1
                    }
                    foreach (var u in game.Team1)
                    {
                        Assert.IsFalse(t2.ContainsKey(u.Username)); // validate every team1 member is not in team 2
                    }

                }
            }
        }

        [TestMethod]
        public void TestValidateInputs()
        {
            var mentions = new List<IUser>();
            for (int i = 0; i < 3; i++)
            {
                var user = new Mock<IUser>();
                user.Setup(u => u.Id).Returns((ulong)i);
                user.Setup(u => u.Username).Returns(i.ToString());
                mentions.Add(user.Object);
            }

            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(null, 1); })); // must have players
            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(mentions, 0); })); // Can not have zero mafia
            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(mentions, -1); })); // Can not have negative mafia
            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(mentions, mentions.Count); })); // Can not have same mafia as players
            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(mentions, mentions.Count + 1); })); // can not have more mafia than players

            // Valid states
            Assert.IsNotNull(TyniBot.Mafia.Game.CreateGame(mentions, 1));
            Assert.IsNotNull(TyniBot.Mafia.Game.CreateGame(mentions, 2));

            mentions.Clear();
            Assert.ThrowsException<Exception>(new Action(() => { TyniBot.Mafia.Game.CreateGame(mentions, 1); })); // Can not have zero players
        }

        [TestMethod]
        public void TestScore2PlayersMafiaLostBothVoteMafia()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });
            // sneak test in which votes for more people than mafia to verify it discards the votes over the number of mafia
            g.Vote(villager.Id, new List<ulong>() { mafia.Id, villager.Id }); 

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(0, 1) : g.Score(1, 0);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 3 + 2 - 1);

            // Villager
            Assert.AreEqual(score[villager.Id], 1 + 2);
        }

        [TestMethod]
        public void TestScore2PlayersMafiaLostNoVoteMafia()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });
            g.Vote(villager.Id, new List<ulong>() { villager.Id });

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(0, 1) : g.Score(1, 0);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 3 + 2);

            // Villager
            Assert.AreEqual(score[villager.Id], 1 + 0);
        }

        [TestMethod]
        public void TestScore2PlayersMafiaWonBothVoteMafia()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });
            g.Vote(villager.Id, new List<ulong>() { mafia.Id });

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(1, 0) : g.Score(0, 1);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 0 + 2 - 1);

            // Villager
            Assert.AreEqual(score[villager.Id], 0 + 2);
        }

        [TestMethod]
        public void TestScore2PlayersMafiaWonNoVoteMafia()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });
            g.Vote(villager.Id, new List<ulong>() { villager.Id });

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(1, 0) : g.Score(0, 1);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 0 + 2);

            // Villager
            Assert.AreEqual(score[villager.Id], 0 + 0);
        }

        [TestMethod]
        public void TestScore2PlayersMafiaWonNoVoteMafiaMafiaDidntVote()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(villager.Id, new List<ulong>() { villager.Id });

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(1, 0) : g.Score(0, 1);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 0 + 2);

            // Villager
            Assert.AreEqual(score[villager.Id], 0 + 0);
        }

        [TestMethod]
        public void TestScore2PlayersMafiaLostNoVoteVillagerMafiaVoteVillager()
        {
            var mentions = new List<IUser>();

            var user1 = new Mock<IUser>();
            user1.Setup(u => u.Username).Returns("k");
            user1.Setup(u => u.Id).Returns(1);
            mentions.Add(user1.Object);

            var user2 = new Mock<IUser>();
            user2.Setup(u => u.Username).Returns("t");
            user2.Setup(u => u.Id).Returns(2);
            mentions.Add(user2.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            var villager = mafia.Id == user1.Object.Id ? user2.Object : user1.Object;
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            // Both vote for Mafia
            g.Vote(mafia.Id, new List<ulong>() { villager.Id });

            // Score such that Mafia lost
            var score = isMafiaTeam1 ? g.Score(0, 1) : g.Score(1, 0);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 3 + 2);

            // Villager
            Assert.AreEqual(score[villager.Id], 1 + 0);
        }

        [TestMethod]
        public void TestScore4PlayersMafiaWonAllGuessedMafia()
        {
            var mentions = new List<IUser>();

            var user1 = GenerateUser("k", 1);
            mentions.Add(user1.Object);

            var user2 = GenerateUser("t", 2);
            mentions.Add(user2.Object);

            var user3 = GenerateUser("j", 3);
            mentions.Add(user3.Object);

            var user4 = GenerateUser("a", 4);
            mentions.Add(user4.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 1);

            var mafia = g.Mafia[0];
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            foreach (var v in g.Villagers)
                g.Vote(v.Id, new List<ulong>() { mafia.Id });

            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });

            // Score such that Mafia won
            var score = isMafiaTeam1 ? g.Score(1, 0) : g.Score(0, 1);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 0); // make sure Mafia score doesn't go below zero
        }

        [TestMethod]
        public void TestScore7PlayersWithJokerEveryoneVote1Mafia1JokerOT()
        {
            var mentions = new List<IUser>();

            var user1 = GenerateUser("k", 1);
            mentions.Add(user1.Object);

            var user2 = GenerateUser("t", 2);
            mentions.Add(user2.Object);

            var user3 = GenerateUser("j", 3);
            mentions.Add(user3.Object);

            var user4 = GenerateUser("a", 4);
            mentions.Add(user4.Object);

            var user5 = GenerateUser("r", 5);
            mentions.Add(user5.Object);

            var user6 = GenerateUser("m", 6);
            mentions.Add(user6.Object);

            var user7 = GenerateUser("n", 7);
            mentions.Add(user7.Object);

            var g = TyniBot.Mafia.Game.CreateGame(mentions, 2, mode:"j");

            var mafias = g.Mafia;
            var t1Mafia = g.Team1.Where(x => g.Mafia.Contains(x)).First();
            var joker = g.Joker;

            // One Mafia on each team and a Joker on the uneven team
            Assert.AreEqual(g.Team1.Where(x => g.Mafia.Contains(x)).Count(), 1);
            Assert.AreEqual(g.Team2.Where(x => g.Mafia.Contains(x)).Count(), 1);
            Assert.AreEqual(g.Team2.Where(x => g.Mafia.Contains(x)).Count(), 1);

            var villagers = g.Villagers;
            foreach (var v in villagers)
                g.Vote(v.Id, new List<ulong>() { t1Mafia.Id, joker.Id });

            foreach (var m in mafias)
                g.Vote(m.Id, new List<ulong>() { t1Mafia.Id, joker.Id });

            g.Vote(joker.Id, new List<ulong>() { t1Mafia.Id, joker.Id });

            // Score such that Team 1 won WITH overtime
            var score = g.Score(1, 0, "ot");

            // Mafia
            Assert.AreEqual(score[t1Mafia.Id], 0); // Team 1 Mafia got guessed and won
            Assert.AreEqual(score[mafias.Where(x => t1Mafia != (x)).First().Id], 5); // Team 2 Mafia lost && no guess
            Assert.AreEqual(score[villagers.Where(x => g.Team1.Contains(x)).First().Id], 3); // Team 1 V won + 1 mafia
            Assert.AreEqual(score[villagers.Where(x => g.Team2.Contains(x)).First().Id], 2); // Team 2 V won + 1 mafia
            Assert.AreEqual(score[joker.Id], 5); // OT + all guesses
        }

        private Mock<IUser> GenerateUser(string username, ulong id)
        {
            var user = new Mock<IUser>();
            user.Setup(u => u.Username).Returns(username);
            user.Setup(u => u.Id).Returns(id);
            return user;
        }
    }
}
