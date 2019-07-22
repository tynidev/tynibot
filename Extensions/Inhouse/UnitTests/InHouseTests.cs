using Discord;
using Discord.Inhouse;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TyniBot;

namespace Inhouse.UnitTests
{
    [TestClass]
    public class InhouseTests
    {
        [TestMethod]
        public async Task TestDbStoreRetrieveInhouseAsync()
        {
            using (var Database = new LiteDatabase(@"test.db"))
            {
                var user1 = new Mock<IUser>();
                user1.Setup(u => u.Username).Returns("bob");
                user1.Setup(u => u.Id).Returns(543);

                var input = new InhouseQueue(987, Player.ToPlayer(user1.Object, 1000));

                var col = Database.GetCollection<InhouseQueue>();

                col.Delete(u => true);
                col.Insert(input);
                col.EnsureIndex(x => x.Id);

                var channelMock = new Mock<IDiscordClient>();
                channelMock.Setup(u => u.GetUserAsync(user1.Object.Id, It.IsAny<CacheMode>(), It.IsAny<RequestOptions>())).Returns(Task.FromResult(user1.Object));

                var output = await InhouseQueue.GetQueueAsync(input.Id, channelMock.Object, col);

                Assert.AreEqual(input.Id, output.Id);
                Assert.AreEqual(input.Players.Count, output.Players.Count);

                Assert.AreEqual(input.Owner.Id, output.Owner.Id);
                Assert.AreEqual(input.Owner.MMR, output.Owner.MMR);

                foreach (var player in input.Players)
                {
                    Assert.IsTrue(output.Players.ContainsKey(player.Key));
                    Assert.AreEqual(player.Value.Id, output.Players[player.Key].Id);
                    Assert.AreEqual(player.Value.MMR, output.Players[player.Key].MMR);
                }
            }
        }
    }
}
