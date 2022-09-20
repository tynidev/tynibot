using Discord.WebSocket;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
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
            if (options.Keys.Where(o => (o & SlashCommandOptions.TeamsFilteringSupport) != SlashCommandOptions.none).Any())
            {
                teams.AddRange(league.Bracket.Teams.Where(t => Matches(t, options)));
            }
            else
            {
                string discordId = $"{user.Username}#{user.Discriminator}";
                if (league?.PlayerDiscordLookup?.ContainsKey(discordId) == true)
                {
                    teams.Add(league.PlayerDiscordLookup[discordId]);
                }
            }

            return teams;
        }

        private static bool Matches(Team team, IReadOnlyDictionary<SlashCommandOptions, string> options)
        {
            bool match = true;

            if (options.ContainsKey(SlashCommandOptions.org) && options[SlashCommandOptions.org] != null)
            {
                match &= team.Org.Contains(options[SlashCommandOptions.org], StringComparison.OrdinalIgnoreCase);
            }

            if (options.ContainsKey(SlashCommandOptions.team) && options[SlashCommandOptions.team] != null)
            {
                match &= team.Name.Contains(options[SlashCommandOptions.team], StringComparison.OrdinalIgnoreCase);
            }

            if (options.ContainsKey(SlashCommandOptions.player) && options[SlashCommandOptions.player] != null)
            {
                match &= team.Players.Where(p => p.DiscordId.Contains(options[SlashCommandOptions.player], StringComparison.OrdinalIgnoreCase)).Any();
            }

            return match; 
        }
    }
}
