using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Linq;

namespace Discord.Cea
{
    public static class TeamResolver
    {
        public static Team ResolveTeam(string team, SocketUser user)
        {
            League league = LeagueManager.League;
            Team t;
            if (team != null)
            {
                t = league.Bracket.Teams.Where(t => t.Name.Contains(team, StringComparison.OrdinalIgnoreCase)).First();
            }
            else
            {
                string discordId = $"{user.Username}#{user.Discriminator}";
                t = league.PlayerDiscordLookup[discordId];
            }

            return t;
        }
    }
}
