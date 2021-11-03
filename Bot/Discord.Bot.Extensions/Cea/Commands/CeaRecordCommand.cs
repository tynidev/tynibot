﻿using Discord.WebSocket;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    internal class CeaRecordCommand : CeaSubCommandMultiTeam
    {
        internal override SlashCommandOptionBuilder OptionBuilder => new SlashCommandOptionBuilder()
        {
            Name = "record",
            Description = "Gets information on the the stage records for a team or teams.",
            Type = ApplicationCommandOptionType.SubCommand
        };

        internal override Embed Run(SocketSlashCommand command, DiscordSocketClient client, IReadOnlyDictionary<SlashCommandOptions, string> options, Team team)
        {
            EmbedBuilder builder = new();
            builder.AddField($"{ team.Name}'s Total Stats:", team.Stats.ToString(true));
            foreach (KeyValuePair<string, TeamStatistics> stats in team.StageStats)
            {
                builder.AddField($"{ stats.Key} Stats:", stats.Value.ToString(true));
            }

            return builder.Build();
        }
    }
}