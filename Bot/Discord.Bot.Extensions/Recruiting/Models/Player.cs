using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Recruiting
{
    public enum Platform
    {
        Epic,
        Steam,
        Xbox,
        Playstation
    }

    public class Player
    {
        public Platform Platform { get; set; } = Platform.Epic;
        public string PlatformId { get; set; } = null;
        public string DiscordUser { get; set; } = null;

        public string TrackerLink()
        {
            var platform = "";
            switch (Platform)
            {
                case Platform.Epic: platform = "epic"; break;
                case Platform.Steam: platform = "steam"; break;
                case Platform.Xbox: platform = "xbl"; break;
                case Platform.Playstation: platform = "psn"; break;
            }

            return $"https://rocketleague.tracker.network/rocket-league/profile/{platform}/{Uri.EscapeUriString(PlatformId)}/overview";
        }

        public static Player ParsePlayer(string line)
        {
            var player = new Player();
            int sep = line.IndexOf(":");
            player.DiscordUser = line.Substring(0, sep - 1);
            var link = line.Substring(sep + 2, line.Length - (sep + 2));

            link = link.Substring(59, link.Length - 9 - 59);
            var parts = link.Split('/');
            player.PlatformId = parts[1];

            switch (parts[0])
            {
                case "epic": player.Platform = Platform.Epic; break;
                case "steam": player.Platform = Platform.Steam; break;
                case "xbl": player.Platform = Platform.Xbox; break;
                case "psn": player.Platform = Platform.Playstation; break;
            }

            return player;
        }

        internal string ToMessage() =>
         $"{this.DiscordUser} : {this.TrackerLink()}\n";
        
    }
}
