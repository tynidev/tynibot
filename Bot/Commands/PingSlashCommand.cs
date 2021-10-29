﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot.Models;

namespace TyniBot.Commands
{
    public class PingSlashCommand : SlashCommand
    {
        public override string Name => "ping";

        public override string Description => "Command to check if bot is running should respond with Pong!";

        public override bool DefaultPermissions => true;

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            await command.RespondAsync("Pong!");
        }
    }
}
