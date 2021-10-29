using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Bot
{
    interface IApplicationCommand
    {
        string Name { get; }

        string Description { get; }

        bool DefaultPermissions { get; }

        ApplicationCommandProperties Build();

        Task<RestApplicationCommand> RegisterCommandAsync(DiscordSocketClient discordSocketClient);
    }
}
