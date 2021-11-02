using Discord;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Bot;
using TyniBot.Recruiting;

namespace Recruiting.UnitTests
{
    [TestClass]
    public class RecruitingTests
    {
        [TestMethod]
        public void TestTeamParseTeam()
        {
            Team expected = new Team();
            expected.Name = "Free Agents";
            expected.Captain = null;
            expected.Players = new List<Player>()
            {
                new Player() { DiscordUser = "tyni", Platform = Platform.Steam, PlatformId = "acuo" },
                new Player() { DiscordUser = "nates321", Platform = Platform.Epic, PlatformId = "xterminates" },
            };

            var msg = expected.ToMessage();

            Team actual = Team.ParseTeam(1, msg);

            Assert.IsTrue(actual.Name == expected.Name);
            Assert.IsTrue(actual.Players.Count() == expected.Players.Count());

            Assert.AreEqual("nates321", actual.Players[0].DiscordUser);
            Assert.AreEqual(Platform.Epic, actual.Players[0].Platform);
            Assert.AreEqual("xterminates", actual.Players[0].PlatformId);

            Assert.AreEqual("tyni", actual.Players[1].DiscordUser);
            Assert.AreEqual(Platform.Steam, actual.Players[1].Platform);
            Assert.AreEqual("acuo", actual.Players[1].PlatformId);
        }
    }
}
