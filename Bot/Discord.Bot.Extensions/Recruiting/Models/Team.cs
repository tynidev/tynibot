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
        public bool LookingForPlayers { get; set; } = false;

        public Player Captain = null;
        public List<Player> Players { get; set; } = new List<Player>();

        public static Team ParseTeam(ulong id, string msg)
        {
            Team team = new Team();
            team.MsgId = id;
            team.Name = msg.Substring(4, msg.IndexOf("**__") - 4);

            StringReader strReader = new StringReader(msg);
            var line = strReader.ReadLine();

            if (string.Equals(line.Substring(msg.IndexOf("**__") + 4).Trim(), "(looking for players)", StringComparison.OrdinalIgnoreCase))
            {
                team.LookingForPlayers = true;
            }

            line = strReader.ReadLine();
            if (team.Name != "Free_Agents")
            {
                string captainName = line.Substring("Captain:".Length).Trim();
                team.Captain = new Player() { DiscordUser = captainName };

                line = strReader.ReadLine();
            }


            while (line != null)
            {
                try
                {
                    team.Players.Add(Player.ParsePlayer(line));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing player {line}");
                }

                line = strReader.ReadLine();
            }

            team.Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            return team;
        }

        public string ToMessage()
        {
            string msg = $"__**{this.Name}**__ { (this.LookingForPlayers ? "(looking for players)" : "")}\n";

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

        public void AddPlayer(Player player)
        {
            Player? existingPlayer = this.Players.Find((p) => string.Equals(p.DiscordUser, player.DiscordUser, StringComparison.OrdinalIgnoreCase));

            if (existingPlayer != default(Player))
            {
                existingPlayer.Platform = player.Platform;
                existingPlayer.PlatformId = player.PlatformId;
            }
            else
            {
                this.Players.Add(player);
            }
        }

        public static Team AddPlayer(List<Team> teams, string teamName, Player player, bool captain = false)
        {
            var team = Team.FindTeam(teams, teamName);

            // Not found? -> Add Free Agent team
            if (team == null)
            {
                team = new Team()
                {
                    Name = teamName,
                    Players = new List<Player>()
                };

                teams.Add(team);
            }

            team.AddPlayer(player);
            
            if (captain)
            {
                team.Captain = player;
            }

            return team;
        }

        public void RemovePlayer(Player player)
        {
            // If player was captain of old team remove that teams captain
            if (this.Captain?.DiscordUser == player.DiscordUser)
                this.Captain = null;

            // Move Player
            this.Players.Remove(player);
        }
    }
}
