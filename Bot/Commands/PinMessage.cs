using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace TyniBot
{
    public class PinMessage : ModuleBase<CommandContext>
    {
        [Command("pin"), Summary("Command to pin the last message to the bottom of the channel!")]
        public async Task Pin()
        {
            var msgCollection = Context.Database.GetCollection<PinnedMessage>();
            var pinnedMsg = msgCollection.Find(m => m.ChannelId == Context.Channel.Id).FirstOrDefault();

            if (pinnedMsg != null) // if message already pinned then ignore
            {
                return;
            }

            // Delete command message
            await Context.Message.DeleteAsync();

            // Get the last message in the channel
            var msgToBePinned = (await Context.Channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();

            // Write the message to the channel
            var msg = await Context.Channel.SendMessageAsync($"**Pinned Message from: {msgToBePinned.Author.Username}\n\n**{msgToBePinned.Content}");

            // Insert pinned message into database
            msgCollection.Insert(new PinnedMessage()
            {
                ChannelId = Context.Channel.Id,
                MsgId = msg.Id,
                Author = msgToBePinned.Author.Username,
                Content = msgToBePinned.Content
            });
            msgCollection.EnsureIndex(x => x.ChannelId);

            return;
        }

        [Command("unpin"), Summary("Command to unpin the last message to the bottom of the channel!")]
        public Task Unpin()
        {
            var msgCollection = Context.Database.GetCollection<PinnedMessage>();
            var pinnedMsg = msgCollection.Find(m => m.ChannelId == Context.Channel.Id).FirstOrDefault();

            if (pinnedMsg == null) // if no message pinned then ignore
            {
                return Task.CompletedTask;
            }

            // Update database
            msgCollection.Delete(m => m.ChannelId == pinnedMsg.ChannelId);

            return Task.CompletedTask;
        }
    }
}
