using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Discord.Bot
{
    public abstract class SlashCommand : IApplicationCommand
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool DefaultPermissions { get; }

        public virtual async Task<RestApplicationCommand> RegisterCommandAsync(DiscordSocketClient discordSocketClient)
        {
            return await discordSocketClient.Rest.CreateGlobalCommand(this.Build());
        }

        public virtual ApplicationCommandProperties Build()
        {

            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);
            return builder.Build();
        }

        public abstract Task HandleCommandAsync(SocketSlashCommand command);
    }
}
