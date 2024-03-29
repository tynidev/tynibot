﻿using Discord.WebSocket;
using PlayCEASharp.DataModel;
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

        SlashCommandOptions ICeaSubCommand.SupportedOptions => SlashCommandOptions.CommonSupport;

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
                embeds.AddRange(Run(command, client, options, t));
            }

            Embed[] embedArray = embeds.Where(e => e != null).Take(10).ToArray();

            await command.RespondAsync(embeds: embedArray, ephemeral:ephemeral);
        }

        internal abstract List<Embed> Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team);

        async Task ICeaSubCommand.Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Lazy<List<Team>> lazyTeams)
        {
            await Run(command, client, options, lazyTeams);
        }
    }
}
