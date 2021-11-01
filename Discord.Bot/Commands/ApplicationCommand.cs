using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Bot
{
    public abstract class ApplicationCommand
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract bool DefaultPermissions { get; }

        public abstract bool IsGlobal { get; }

        public abstract Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions { get; }

        public abstract ApplicationCommandProperties Build();
    }
}
