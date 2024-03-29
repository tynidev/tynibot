﻿using Discord.Bot;
using Discord.Bot.Utils;
using Discord.WebSocket;
using PlayCEASharp.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public class CeaNextUserCommand : CeaUserCommand
    {
        public override string Name => "CEA Next";

        public override async Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient, List<Team> teams)
        {
            await command.RespondAsync(embeds:teams.SelectMany(t => CeaNextCommand.GetEmbeds(t)).ToArray(), ephemeral:true);
        }
    }
}
