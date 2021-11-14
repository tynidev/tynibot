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
        public static IReadOnlyDictionary<SlashCommandOptions, string> OptionsToDictionary(IReadOnlyCollection<SocketSlashCommandDataOption> options)
        {
            Dictionary<SlashCommandOptions, string> optionsDictionary = new();

            foreach(SocketSlashCommandDataOption option in options)
            {
                SlashCommandOptions optionEnum = (SlashCommandOptions) Enum.Parse(typeof(SlashCommandOptions), option.Name);

                if (option.Value is IUser)
                {
                    optionsDictionary[optionEnum] = $"{(option.Value as IUser).Username}#{(option.Value as IUser).Discriminator}";
                } 
                else
                {
                    optionsDictionary[optionEnum] = option.Value.ToString();
                }
            }

            return optionsDictionary;
        }

        public static void AddCommonOptionProperties(SlashCommandOptionBuilder optionBuilder, SlashCommandOptions supportedOptions)
        {
            if (supportedOptions.HasFlag(SlashCommandOptions.team))
            {
                optionBuilder.AddOption(SlashCommandOptions.team.ToString(),
                            ApplicationCommandOptionType.String,
                            "Filter command option to a specific team name.");
            }

            if (supportedOptions.HasFlag(SlashCommandOptions.org))
            {
                optionBuilder.AddOption(
                    name: SlashCommandOptions.org.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific organization (company).");
            }

            if (supportedOptions.HasFlag(SlashCommandOptions.player))
            {
                optionBuilder.AddOption(
                    name: SlashCommandOptions.player.ToString(),
                    type: ApplicationCommandOptionType.String,
                    description: "Filter command option to a specific player.");
            }

            if (supportedOptions.HasFlag(SlashCommandOptions.week))
            {
                optionBuilder.AddOption(
                    name: SlashCommandOptions.week.ToString(),
                    type: ApplicationCommandOptionType.Integer,
                    description: "Display information for a specific week of the bracket.");
            }

            if (supportedOptions.HasFlag(SlashCommandOptions.post))
            {
                optionBuilder.AddOption(
                    name: SlashCommandOptions.post.ToString(),
                    type: ApplicationCommandOptionType.Boolean,
                    description: "Respond publicly instead of ephemerally.");
            }
        }
    }
}
