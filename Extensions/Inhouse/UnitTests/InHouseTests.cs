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
        public void Test()
        {
            using (var Database = new LiteDatabase(@"test.db"))
            {
            }
        }

        [TestMethod]
        public void TestDbStoreRetrieveGame()
        {
            using (var Database = new LiteDatabase(@"test.db"))
            {

            }
        }
    }
}
