using Discord;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using TyniBot;
using TyniBot.Models;

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

                var input = MafiaGame.CreateGame(mentions, 1).Game;
                input.Id = 1;

                var gamesCollection = Database.GetCollection<MafiaGame>();
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
            for (int j = 0; j < 16; j++)
            {
                var mentions = new List<IUser>();
                for (int i = 0; i < (j % 7) + 2; i++)
                {
                    var user = new Mock<IUser>();
                    user.Setup(u => u.Id).Returns((ulong)i);
                    user.Setup(u => u.Username).Returns(i.ToString());
                    mentions.Add(user.Object);
                }

                for (int i = 0; i < 100; i++)
                {
                    var numMafia = (i % (mentions.Count - 1)) + 1;
                    var game = (MafiaGame.CreateGame(mentions, numMafia)).Game;

                    Assert.AreEqual(numMafia, game.Mafia.Count()); // validate actual number of mafia was as requested
                    Assert.AreEqual(game.Team1.Count() + game.Team2.Count(), mentions.Count); // validate members of both teams equals total count of mentions

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

            Assert.IsNotNull(MafiaGame.CreateGame(null, 1).ErrorMsg); // must have players
            Assert.IsNotNull(MafiaGame.CreateGame(mentions, 0).ErrorMsg); // Can not have zero mafia
            Assert.IsNotNull(MafiaGame.CreateGame(mentions, -1).ErrorMsg); // Can not have negative mafia
            Assert.IsNotNull(MafiaGame.CreateGame(mentions, mentions.Count).ErrorMsg); // Can not have same mafia as players
            Assert.IsNotNull(MafiaGame.CreateGame(mentions, mentions.Count + 1).ErrorMsg); // can not have more mafia than players

            // Valid states
            Assert.IsNull(MafiaGame.CreateGame(mentions, 1).ErrorMsg);
            Assert.IsNull(MafiaGame.CreateGame(mentions, 2).ErrorMsg);

            mentions.Clear();
            Assert.IsNotNull(MafiaGame.CreateGame(mentions, 1).ErrorMsg); // Can not have zero players
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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

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

            var g = MafiaGame.CreateGame(mentions, 1).Game;

            var mafia = g.Mafia[0];
            bool isMafiaTeam1 = g.Team1.Where(u => u.Id == mafia.Id).Count() > 0;

            var villagers = g.Players.Where(u => u.Key != mafia.Id);
            foreach (var v in villagers)
                g.Vote(v.Key, new List<ulong>() { mafia.Id });

            g.Vote(mafia.Id, new List<ulong>() { mafia.Id });

            // Score such that Mafia won
            var score = isMafiaTeam1 ? g.Score(1, 0) : g.Score(0, 1);

            // Mafia
            Assert.AreEqual(score[mafia.Id], 0); // make sure Mafia score doesn't go below zero
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
