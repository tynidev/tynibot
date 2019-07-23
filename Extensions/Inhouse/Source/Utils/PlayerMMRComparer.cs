using System.Collections.Generic;

namespace Discord.Inhouse
{
    public class PlayerMMRComparer : Comparer<Player>
    {
        public override int Compare(Player x, Player y)
        {
            return x.MMR.CompareTo(y.MMR);
        }
    }
}
