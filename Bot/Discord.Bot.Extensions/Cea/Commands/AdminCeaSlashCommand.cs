using Discord.Bot;
using Discord.WebSocket;
using PlayCEA_RL.Configuration;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public class AdminCeaSlashCommand : SlashCommand
    {
        public override string Name => "admincea";

        public override string Description => "Command to update PlayCea Configuration for the bot.";

        public override bool DefaultPermissions => false;

        public override bool IsGlobal => false;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            { 902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) } }, // tynibot test employee
            { 124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(469941381075435523, ApplicationCommandPermissionTarget.Role, true) } }, // msft rl admin
        };

        public AdminCeaSlashCommand() : base()
        {
        }

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            var subCommand = command.Data.Options.Where(o => o.Type.Equals(ApplicationCommandOptionType.SubCommand)).First();
            string configuration = (string) subCommand.Options.Where(o => o.Name.Equals("configuration")).First().Value;

            try
            {
                BracketConfiguration newConfiguration = JsonSerializer.Deserialize<BracketConfiguration>(configuration);
                ConfigurationManager.UpdateInMemoryConfiguration(newConfiguration);
                await command.RespondAsync($"New Configuration Set.\n{configuration}", ephemeral: true);
                LeagueManager.ForceUpdate();
            } 
            catch (JsonException)
            {
                await command.RespondAsync("Invalid BracketConfiguration JSON.", ephemeral: true);
                throw;
            }
            catch (Exception e)
            {
                await command.RespondAsync($"Unknown Exception.\n{e}", ephemeral: true);
                throw;
            }
        }

        public override ApplicationCommandProperties Build()
        {
            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);

            SlashCommandOptionBuilder optionBuilder = new SlashCommandOptionBuilder()
            {
                Name = "configure",
                Description = "Gets all match history for a team or teams.",
                Type = ApplicationCommandOptionType.SubCommand
            };

            optionBuilder.AddOption(
                    name: "configuration",
                    type: ApplicationCommandOptionType.String,
                    description: "The full JSON configuration for the CEA bot.",
                    required: true);
            

            builder.AddOption(optionBuilder);

            return builder.Build();
        }
    }
}
