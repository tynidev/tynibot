using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace TyniBot
{
    public interface IChannelHandler
    {
        Task<IResult> MessageReceived(CommandContext context);
    }
}