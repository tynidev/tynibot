using Discord.Commands;
using System.Threading.Tasks;

namespace TyniBot
{
    public interface IChannelHandler
    {
        Task<IResult> MessageReceived(TyniCommandContext context);
    }
}