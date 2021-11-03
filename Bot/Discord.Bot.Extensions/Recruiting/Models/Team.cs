using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Recruiting
{
    public class Team
    {
        public ulong MsgId { get; set; } = 0;
        public string Name { get; set; } = null;
        public Player Captain = null;
        public List<Player> Players { get; set; } = new List<Player>();

        public static Team ParseTeam(ulong id, string msg)
        {
            Team team = new Team();
            team.MsgId = id;
            team.Name = msg.Substring(4, msg.IndexOf("**__") - 4);

            StringReader strReader = new StringReader(msg);
            strReader.ReadLine();

            var line = strReader.ReadLine();

            if (team.Name != "Free_Agents")
            {
                string captainName = line.Substring("Captain:".Length).Trim();
                team.Captain = new Player() { DiscordUser = captainName };

                line = strReader.ReadLine();
            }

            while (line != null)
            {
                team.Players.Add(Player.ParsePlayer(line));
                line = strReader.ReadLine();
            }

            team.Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            return team;
        }

        public string ToMessage()
        {
            string msg = $"__**{this.Name}**__\n";

            if (this.Name != "Free_Agents")
            {
                msg += $"Captain: {(this.Captain != null ? $"{this.Captain.DiscordUser}" : " ")}\n";
            }
            Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            foreach (var player in Players)
            {
                msg += player.ToMessage();
            }

            return msg;
        }

        public Player FindPlayer(string discordUser)
        {
            var exists = Players.Where((p) => string.Equals(p.DiscordUser, discordUser, StringComparison.OrdinalIgnoreCase));
            if (exists.Any())
            {
                return exists.First();
            }
            return null;
        }

        public static (Team, Player) FindPlayer(IEnumerable<Team> teams, string discordUser)
        {
            foreach (var team in teams)
            {
                var player = team.FindPlayer(discordUser);
                if (player != null)
                {
                    return (team, player);
                }
            }
            return (null, null);
        }

        public static Team FindTeam(IEnumerable<Team> teams, string teamName)
        {
            foreach (var team in teams)
            {
                if (string.Equals(team.Name, teamName, StringComparison.OrdinalIgnoreCase))
                    return team;
            }
            return null;
        }
    }
}
