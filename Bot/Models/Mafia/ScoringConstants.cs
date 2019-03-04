using System;
using System.Collections.Generic;
using System.Text;

namespace TyniBot.Mafia
{
    static class ScoringConstants
    {
        public static int GuessingMafia = 2; // two points for each correct vote as villager
        public static int WinningGame = 1; // one point for winning as villager
        public static int LosingAsMafia = 3; // three points for losing as mafia
        public static int MaxHiddenAsMafia = 2; // max points staying hidden as mafia
        public static int ReachingOvertime = 2; // three points for going to overtime as joker
        public static int MaxMafiaGuessAsJoker = 3; // max points being guessed as mafia as joker
    }
}
