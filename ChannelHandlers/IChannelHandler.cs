using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace TyniBot
{
    public interface IChannelHandler
    {
        Task Execute(CommandContext context, ServiceProvider serviceProvider);
    }
}