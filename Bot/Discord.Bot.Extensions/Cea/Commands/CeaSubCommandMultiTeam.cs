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

        SlashCommandOptionBuilder ICeaSubCommand.OptionBuilder => OptionBuilder;

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.TeamsFilteringSupport;

        internal async Task Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams) 
        {
            bool ephemeral = !options.ContainsKey(SlashCommandOptions.post) || !options[SlashCommandOptions.post].Equals("True");

            List<Embed> embeds = new();

            if (lazyTeams.Value.Count == 0)
            {
                await command.RespondAsync("No teams matched the given criteria.", ephemeral:ephemeral);
                return;
            }

            foreach (Team t in lazyTeams.Value)
            {
                embeds.Add(Run(command, client, options, t));
            }

            await command.RespondAsync(embeds: embeds.ToArray(), ephemeral:ephemeral);
        }

        internal abstract Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team);

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            await Run(command, client, options, lazyTeams);
        }
    }
}
