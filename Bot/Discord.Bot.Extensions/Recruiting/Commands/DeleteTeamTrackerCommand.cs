﻿using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;
using Discord.Bot;
using System;
using TyniBot.Recruiting;

namespace TyniBot.Commands
{
    // Todo: store guild Ids, role ids, and channel ids in permanent external storage to allow for servers to configure their addtracker command 
    public class DeleteTeamTrackerCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var teamName = options["team"].Value.ToString();

            // Player not exist? -> respond with error
            var team = Team.FindTeam(teams, teamName);
            if (team == null)
            {
                await command.RespondAsync($"Team {teamName} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            // Remove old team message
            await recruitingChannel.DeleteMessageAsync(team.MsgId);
            await command.RespondAsync($"You have removed team {teamName}", ephemeral: true);
        }
    }
}