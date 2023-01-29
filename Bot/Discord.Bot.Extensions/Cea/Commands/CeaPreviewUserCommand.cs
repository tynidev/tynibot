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
    public class CeaPreviewUserCommand : CeaUserCommand
    {
        public override string Name => "CEA Preview";

        public override async Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient, List<Team> teams)
        {
            Embed[] response = teams.SelectMany(t => CeaPreviewCommand.GetEmbed(t)).Where(e => e != null).ToArray();

            if (response == null || response.Length == 0)
            {
                await command.RespondAsync($"No next match found for {command.User.Username}.");
                return;
            }

            await command.RespondAsync(embeds:response, ephemeral:true);
        }
    }
}
