using System;
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
    public abstract class UserCommand : ApplicationCommand
    {
        public override ApplicationCommandProperties Build()
        {

            var builder = new UserCommandBuilder()
                    .WithName(this.Name)
                    .WithDefaultPermission(this.DefaultPermissions);
            return builder.Build();
        }

        public abstract Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient);
    }
}
