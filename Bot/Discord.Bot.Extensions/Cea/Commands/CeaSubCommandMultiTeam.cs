using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    abstract class CeaSubCommandMultiTeam : ICeaSubCommand
    {
        internal abstract SlashCommandOptionBuilder OptionBuilder { get; }
        internal abstract SlashCommandOptions SupportedOptions { get; }

        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => OptionBuilder;

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SupportedOptions;

        internal async Task Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams) 
        {
            List<Embed> embeds = new();

            if (lazyTeams.Value.Count == 0)
            {
                await command.RespondAsync("No teams matched the given criteria.", ephemeral:true);
                return;
            }

            foreach (Team t in lazyTeams.Value)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = t.Name
                };
                embedBuilder.AddField(t.Name, Run(command, client, options, t));
                embeds.Add(embedBuilder.Build());
            }

            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral:true);
        }

        internal abstract string Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team);

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            await Run(command, client, options, lazyTeams);
        }
    }
}
