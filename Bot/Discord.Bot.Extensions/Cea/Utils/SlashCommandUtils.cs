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
        public static void AddCommonArguments(SlashCommandOptionBuilder builder, SlashCommandOptions slashCommandOptions)
        {
            if (slashCommandOptions.HasFlag(SlashCommandOptions.team))
            {
                builder.AddOption(
                    name: SlashCommandOptions.team.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific team.");
            }

            if (slashCommandOptions.HasFlag(SlashCommandOptions.org))
            {
                builder.AddOption(
                    name: SlashCommandOptions.org.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific organization (company).");
            }
        }

        public static IReadOnlyDictionary<SlashCommandOptions, string> OptionsToDictionary(IReadOnlyCollection<SocketSlashCommandDataOption> options)
        {
            Dictionary<SlashCommandOptions, string> optionsDictionary = new();

            foreach(SocketSlashCommandDataOption option in options)
            {
                SlashCommandOptions optionEnum = (SlashCommandOptions) Enum.Parse(typeof(SlashCommandOptions), option.Name);
                optionsDictionary[optionEnum] = (string)option.Value;
            }

            return optionsDictionary;
        }
    }
}
