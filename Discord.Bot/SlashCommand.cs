using System;
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

        public virtual bool IsGlobal => true;

        public Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions = new Dictionary<ulong, List<ApplicationCommandPermission>>();


        public virtual SlashCommandProperties CreateSlashCommand() =>
            new SlashCommandBuilder()
                   .WithName(this.Name)
                   .WithDescription(this.Description)
                   .WithDefaultPermission(this.DefaultPermissions)
                   .Build();

        public abstract Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client);
    }
}
