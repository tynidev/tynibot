using Discord;
using Discord.Bot;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Commands
{
    public class PingSlashCommand : SlashCommand
    {
        public override string Name => "ping";

        public override string Description => "Command to check if bot is running should respond with Pong!";

        public override bool DefaultPermissions => true;

        public override bool IsGlobal => true;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>();

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            await command.RespondAsync("Pong!");
        }
    }
}
