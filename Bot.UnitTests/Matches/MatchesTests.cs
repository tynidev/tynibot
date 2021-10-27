using Discord.Matches;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Matches.UnitTests
{
    [TestClass]
    public class MatchesTests
    {
        [TestMethod]
        public void Test6Player()
        {
            var players = GeneratePlayers(6);
            var matches = MatchesCommand.GetUniqueMatches(players);

            VerifyMatch(matches, players, 10);
        }

        [TestMethod]
        public void Test4Player()
        {
            var players = GeneratePlayers(4);
            var matches = MatchesCommand.GetUniqueMatches(players);

            VerifyMatch(matches, players, 3);
        }

        [TestMethod]
        public void Test2Player()
        {
            var players = GeneratePlayers(2);
            var matches = MatchesCommand.GetUniqueMatches(players);

            VerifyMatch(matches, players, 1);
        }

        [TestMethod]
        public void TestErrors()
        {
            Action act = () => MatchesCommand.GetUniqueMatches(GeneratePlayers(3));
            act.Should().Throw<Exception>();

            act = () => MatchesCommand.GetUniqueMatches(GeneratePlayers(1));
            act.Should().Throw<Exception>();

            act = () => MatchesCommand.GetUniqueMatches(GeneratePlayers(0));
            act.Should().Throw<Exception>();

            act = () => MatchesCommand.GetUniqueMatches(GeneratePlayers(8));
            act.Should().Throw<Exception>();

            var players = GeneratePlayers(1);
            players.Add(string.Empty);
            players.Add(null);
            players.Add("   ");
            act = () => MatchesCommand.GetUniqueMatches(players);
            act.Should().Throw<Exception>();

            players = GeneratePlayers(0);
            players.Add("1");
            players.Add("1");
            players.Add("1");
            players.Add("1");
            act = () => MatchesCommand.GetUniqueMatches(players);
            act.Should().Throw<Exception>();
        }

        private void VerifyMatch(List<Tuple<List<string>, List<string>>> matches, List<string> players, int expectedMatches)
        {
            Assert.IsNotNull(matches, "matches was null");
            Assert.AreEqual(matches.Count, expectedMatches, $"{matches.Count} total matches did not match expected count {expectedMatches}.");

            for (int i = 0; i < matches.Count; i++)
            {
                var m1 = matches[i];

                // verify total players per match is same as all players
                Assert.AreEqual(players.Count, m1.Item1.Count + m1.Item2.Count, "Match doesn't contain the right number of players."); 
                
                // verify teams are split evenly
                Assert.AreEqual(players.Count / 2, m1.Item1.Count(), "Teams aren't split evenly.");
                Assert.AreEqual(players.Count / 2, m1.Item2.Count(), "Teams aren't split evenly.");

                // verify no-one from team1 is on team 2
                Assert.IsTrue(m1.Item1.ContainsNone(m1.Item2), "Teams are not unique.");

                // verify all players are in every match
                foreach (var player in players)  
                    Assert.IsTrue(m1.Item1.Contains(player) || m1.Item2.Contains(player), $"Player {player} wasn't in the match.");

                // verify uniqueness of match
                for (int j = 0; j < matches.Count; j++)
                {
                    if (i == j) continue;

                    var m2 = matches[j];
                    Assert.IsFalse(m1.Item1.ContainsAll(m2.Item1), "Matches are not unique.");
                    Assert.IsFalse(m1.Item1.ContainsAll(m2.Item2), "Matches are not unique.");
                    Assert.IsFalse(m1.Item2.ContainsAll(m2.Item1), "Matches are not unique.");
                    Assert.IsFalse(m1.Item2.ContainsAll(m2.Item2), "Matches are not unique.");
                }
            }
        }

        public List<string> GeneratePlayers(int playerCount)
        {
            var players = new List<string>();
            for (int i = 0; i < playerCount; i++)
                players.Add(i.ToString());
            return players;
        }
    }
}
