using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{ 
    class SlashCommandUtils
    {
        public static void AddCommonArguments(SlashCommandBuilder builder, SlashCommandOptions slashCommandOptions)
        {
            if (slashCommandOptions.HasFlag(SlashCommandOptions.Team))
            {
                builder.AddOption(
                    name: SlashCommandOptions.Team.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific team.",
                    required: false);
            }

            if (slashCommandOptions.HasFlag(SlashCommandOptions.Org))
            {
                builder.AddOption(
                    name: SlashCommandOptions.Org.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific organization (company).",
                    required: false);
            }
        }

        public static IReadOnlyDictionary<SlashCommandOptions, string> OptionsToDictionary(SocketSlashCommandData data)
        {
            Dictionary<SlashCommandOptions, string> options = new();

            foreach(SocketSlashCommandDataOption option in data.Options)
            {
                SlashCommandOptions optionEnum = (SlashCommandOptions) Enum.Parse(typeof(SlashCommandOptions), option.Name);
                options[optionEnum] = (string)option.Value;
            }

            return options;
        }
    }
}
