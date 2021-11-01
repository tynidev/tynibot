﻿using System;
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

            Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            foreach (var player in Players)
            {
                msg += $"{player.DiscordUser} : {player.TrackerLink()}\n";
            }

            return msg;
        }
        public Player FindPlayer(string discordUser)
        {
            var exists = Players.Where((p) => p.DiscordUser == discordUser);
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
                if (team.Name == teamName)
                    return team;
            }
            return null;
        }
    }
}
