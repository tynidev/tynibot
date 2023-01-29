using Discord.Bot;
using Discord.Bot.Utils;
using Discord.WebSocket;
using PlayCEASharp.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public class CeaHistoryUserCommand : CeaUserCommand
    {
        public override string Name => "CEA History";

        public override async Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient, List<Team> teams)
        {
            await command.RespondAsync(embeds: teams.SelectMany(t => CeaHistoryCommand.GetEmbed(t)).ToArray(), ephemeral:true);
        }
    }
}
