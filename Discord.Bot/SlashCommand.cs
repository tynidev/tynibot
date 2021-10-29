﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Discord.Bot
{
    public abstract class SlashCommand
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool DefaultPermissions { get; }

        public virtual SlashCommandProperties CreateSlashCommand()
        {

            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);
            this.AddOptions(builder);
            return builder.Build();
        }

        public abstract Task HandleCommandAsync(SocketSlashCommand command);

        public virtual void AddOptions(SlashCommandBuilder builder) { }
    }
}
