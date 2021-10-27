using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Inhouse
{
    public class TeamComparer : Comparer<Tuple<List<Player>, List<Player>>>
    {
        public override int Compare(Tuple<List<Player>, List<Player>> x, Tuple<List<Player>, List<Player>> y)
        {
            return MMRDifference(x).CompareTo(MMRDifference(y));
        }

        private int MMRDifference(Tuple<List<Player>, List<Player>> match)
        {
            // Foreach might be faster.

            int mmrFirstTeam = match.Item1.Sum(item => item.MMR);

            int mmrSecondTeam = match.Item2.Sum(item => item.MMR);

            return Math.Abs(mmrFirstTeam - mmrSecondTeam);
        }
    }
}
