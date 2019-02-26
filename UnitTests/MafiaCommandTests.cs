using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using TyniBot;

namespace UnitTests
{
    [TestClass]
    public class MafiaCommandTests
    {
        [TestMethod]
        public void TestCreateGameGeneratesValidGame()
        {
            for (int j = 0; j < 16; j++)
            {
                var mentions = new List<IUser>();
                for (int i = 0; i < (j % 7) + 2; i++)
                {
                    var user = new Mock<IUser>();
                    user.Setup(u => u.Username).Returns(i.ToString());
                    mentions.Add(user.Object);
                }

                for (int i = 0; i < 100; i++)
                {
                    var numMafia = (i % (mentions.Count - 1)) + 1;
                    var game = Mafia.CreateGame(mentions, numMafia);

                    Assert.AreEqual(game.Mafia.Count(), numMafia);
                    Assert.AreEqual(game.Team1.Count() + game.Team2.Count(), mentions.Count);

                    var mafia = new Dictionary<string, string>();
                    var t1 = new Dictionary<string, string>();
                    var t2 = new Dictionary<string, string>();

                    foreach (var u in game.Mafia)
                    {
                        Assert.IsTrue(mentions.Contains(u));
                        Assert.IsFalse(mafia.ContainsKey(u.Username));
                        mafia.Add(u.Username, u.Username);
                    }
                    foreach (var u in game.Team1)
                    {
                        t1.Add(u.Username, u.Username);
                        Assert.IsTrue(mentions.Contains(u));
                    }
                    foreach (var u in game.Team2)
                    {
                        t2.Add(u.Username, u.Username);
                        Assert.IsTrue(mentions.Contains(u));
                        Assert.IsFalse(t1.ContainsKey(u.Username));
                    }
                    foreach (var u in game.Team1)
                    {
                        Assert.IsFalse(t2.ContainsKey(u.Username));
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
                user.Setup(u => u.Username).Returns(i.ToString());
                mentions.Add(user.Object);
            }

            Assert.IsNotNull(Mafia.ValidateCommandInputs(null, 1)); // must have palyers
            Assert.IsNotNull(Mafia.ValidateCommandInputs(mentions, 0)); // Can not have zero mafia
            Assert.IsNotNull(Mafia.ValidateCommandInputs(mentions, -1)); // Can not have negative mafia
            Assert.IsNotNull(Mafia.ValidateCommandInputs(mentions, mentions.Count)); // Can not have same mafia as players
            Assert.IsNotNull(Mafia.ValidateCommandInputs(mentions, mentions.Count + 1)); // can not have more mafia than players

            // Valid states
            Assert.IsNull(Mafia.ValidateCommandInputs(mentions, 1));
            Assert.IsNull(Mafia.ValidateCommandInputs(mentions, 2));

            mentions.Clear();
            Assert.IsNotNull(Mafia.ValidateCommandInputs(mentions, 1)); // Can not have zero players
        }
    }
}
