using Discord.Bot;
using Discord.WebSocket;
using PlayCEAStats.DataModel;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    public class CeaTeamSlashCommand : SlashCommand
    {
        public override string Name => "cea team";

        public override string Description => "Command to see info from PlayCea.com.";

        public override bool DefaultPermissions => true;

        public override bool IsGlobal => true;

        public override Dictionary<ulong, List<ApplicationCommandPermission>> GuildIdsAndPermissions => new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
        };

        public override async Task HandleCommandAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            IReadOnlyDictionary<SlashCommandOptions, string> options = SlashCommandUtils.OptionsToDictionary(command.Data);

            List<Team> teams = TeamResolver.ResolveTeam(options, command.User);
            StringBuilder sb = new();

            foreach (Team t in teams)
            {
                sb.AppendLine($"Team: {t.Name}, Current Rank: {t.Rank} [{t.Stats.MatchWins}-{t.Stats.MatchLosses}]");
                sb.AppendLine($"Goal Differential: {t.Stats.TotalGoalDifferential}, Goals/Game: {(double)t.Stats.TotalGoals / t.Stats.TotalGames}");
                foreach (Player p in t.Players)
                {
                    string captainTag = p.Captain ? "(c) " : "";
                    sb.AppendLine($"{captainTag} {p.DiscordId}");
                }
            }

            await command.RespondAsync(sb.ToString());
        }

        public override ApplicationCommandProperties Build()
        {
            var builder = new SlashCommandBuilder()
                    .WithName(this.Name)
                    .WithDescription(this.Description)
                    .WithDefaultPermission(this.DefaultPermissions);

            SlashCommandUtils.AddCommonArguments(builder,
                SlashCommandOptions.Team
                | SlashCommandOptions.Org);

            return builder.Build();
        }
    }
}
