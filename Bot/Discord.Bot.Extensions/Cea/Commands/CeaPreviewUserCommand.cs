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

        public override async Task HandleCommandAsync(SocketUserCommand command, DiscordSocketClient client, StorageClient storageClient, Team team)
        {
            Embed response = CeaPreviewCommand.GetEmbed(team);

            if (response == null)
            {
                await command.RespondAsync($"No next match found for {team.Name}.");
                return;
            }

            await command.RespondAsync(embeds:new Embed[] { response }, ephemeral:true);
        }
    }
}
