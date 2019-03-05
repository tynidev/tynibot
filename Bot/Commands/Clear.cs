using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Clear : ModuleBase<CommandContext>
    {
        [Command("clear"), Summary("!clear | Clears the entire channel if you hvae owner permissions.")]
        public async Task clear()
        {
            if (Context.User.Id != Context.Guild.Owner.Id)
                return;

            var messages = (await Context.Channel.GetMessagesAsync().FlattenAsync());
            foreach(var msg in messages)
            {
                await msg.DeleteAsync();
            }
        }
    }
}
