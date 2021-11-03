using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    interface ICeaSubCommand
    {
        internal SlashCommandOptionBuilder OptionBuilder { get; }
        internal SlashCommandOptions SupportedOptions { get; }

        internal Task Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams);
    }
}
