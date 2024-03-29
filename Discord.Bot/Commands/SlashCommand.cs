﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Bot.Utils;

namespace Discord.Bot
{
    public abstract class SlashCommand : ApplicationCommand
    {
        public abstract string Description { get; }

        public override ApplicationCommandProperties Build()
        {

            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);
            return builder.Build();
        }

        public abstract Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client, StorageClient storageClient);
    }
}
