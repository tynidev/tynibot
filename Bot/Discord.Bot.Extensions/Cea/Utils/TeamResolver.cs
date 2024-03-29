﻿using Discord.WebSocket;
using PlayCEASharp.DataModel;
using PlayCEASharp.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Cea
{
    public static class TeamResolver
    {
        internal static List<Team> ResolveUsersTeam(SocketUser user)
        {
            string discordId = $"{user.Username}#{user.Discriminator}";
            return LeagueManager.PlayerLookup.GetValueOrDefault(discordId, null);
        }

        internal static List<Team> ResolveTeam(IReadOnlyDictionary<SlashCommandOptions, string> options, SocketUser user)
        {
            List<Team> teams = new();
            foreach (League league in LeagueManager.Leagues)
            {
                if (options.Keys.Where(o => (o & SlashCommandOptions.TeamsFilteringSupport) != SlashCommandOptions.none).Any())
                {
                    teams.AddRange(league.Bracket.Teams.Where(t => Matches(t, options)));
                }
                else
                {
                    string discordId = $"{user.Username}#{user.Discriminator}";
                    teams.AddRange(league.Bracket.Teams.Where(t => HasPlayer(t, discordId)));
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

        private static bool HasPlayer(Team team, string player)
        {
            return team.Players.Where(p => p.DiscordId.Equals(player, StringComparison.OrdinalIgnoreCase)).Any();
        }
    }
}
