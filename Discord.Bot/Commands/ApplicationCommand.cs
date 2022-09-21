using Discord.Bot.Utils;
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

        public abstract bool DefaultPermissions { get; }

        public abstract bool IsGlobal { get; }

        public virtual Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => GuildIdMappings.defaultSlashCommandPermissions;

        public abstract ApplicationCommandProperties Build();
    }
}
