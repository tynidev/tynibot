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
    public class LookingForPlayersCommand
    {
        public static async Task Run(SocketSlashCommand command, DiscordSocketClient client, Dictionary<string, SocketSlashCommandDataOption> options, ISocketMessageChannel recruitingChannel, List<IMessage> messages, List<Team> teams)
        {
            var teamName = options["team"].Value.ToString();
            var lookingForPlayers = (bool)options["looking"].Value;

            // Player not exist? -> respond with error
            var team = Team.FindTeam(teams, teamName);
            if (team == null)
            {
                await command.RespondAsync($"Team {teamName} does not exist in the recruiting table", ephemeral: true);
                return;
            }

            team.LookingForPlayers = lookingForPlayers;

            await recruitingChannel.ModifyMessageAsync(team.MsgId, (message) => message.Content = team.ToMessage());
            await command.RespondAsync($"You marked team {team.Name} as looking for players {lookingForPlayers}", ephemeral: true);
        }
    }
}
