using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Mafia
{
    public static class ScoringConstants
    {
        public const int GuessedMafia = 2; // two points for each correct vote as villager
        public const int WinningGame = 1; // one point for winning as villager
        public const int LosingAsMafia = 3; // three points for losing as mafia
        public const int MafiaNobodyGuessedMe = 2; // max points staying hidden as mafia
        public const int ReachedOvertime = 2; // three points for going to overtime as joker
        public const int JokerGuessedAsMafiaMax = 3; // max points for Joker being guessed as mafia
    }
}
