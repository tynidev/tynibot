using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Cea
{
    public static class TeamResolver
    {
        internal static Team ResolveTeam(string team, SocketUser user)
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

        internal static List<Team> ResolveTeam(IReadOnlyDictionary<SlashCommandOptions, string> options, SocketUser user)
        {
            League league = LeagueManager.League;
            List<Team> teams = new();
            if (!options.ContainsKey(SlashCommandOptions.Team) && !options.ContainsKey(SlashCommandOptions.Org))
            {
                string discordId = $"{user.Username}#{user.Discriminator}";
                teams.Add(league.PlayerDiscordLookup[discordId]);
            }
            else
            {
                teams.AddRange(league.Bracket.Teams.Where(t => Matches(t, options)));
            }

            return teams;
        }

        private static bool Matches(Team team, IReadOnlyDictionary<SlashCommandOptions, string> options)
        {
            bool match = true;

            if (options.ContainsKey(SlashCommandOptions.Org) && options[SlashCommandOptions.Org] != null)
            {
                match &= team.Org.Contains(options[SlashCommandOptions.Org], StringComparison.OrdinalIgnoreCase);
            }

            if (options.ContainsKey(SlashCommandOptions.Team) && options[SlashCommandOptions.Team] != null)
            {
                match &= team.Name.Contains(options[SlashCommandOptions.Team], StringComparison.OrdinalIgnoreCase);
            }

            return match; 
        }
    }
}
