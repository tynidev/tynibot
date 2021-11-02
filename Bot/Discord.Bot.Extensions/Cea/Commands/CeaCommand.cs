using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using PlayCEAStats.Analysis;
using PlayCEAStats.DataModel;
using PlayCEAStats.RequestManagement;

namespace Discord.Cea
{
	[Group("cea")]
	public class CeaCommand : ModuleBase<Discord.Bot.CommandContext>
	{
		// !cea next -> Clippit's next match is against Ninjacats.
		[Command("next")]
		[Summary("Looks up the discord user to show their next opponents.")]
		[Alias("n")]
		public async Task NextMatchAsync(
			[Summary("The (optional) team to get info for")] string team = null)
		{
			League league = LeagueManager.League;

			Team t = TeamResolver.ResolveTeam(team, Context.User);

			MatchResult match = league.NextMatchLookup[t];

			string message = string.Format("{0}'s next match is in week {1}, {2} vs {3}.{4}",
				t, match.Round + 1, match.HomeTeam, match.AwayTeam, match.Completed ? " (Completed)" : "");
			await ReplyAsync(message);
		}


		[Command("record")]
		[Summary("Returns info about your teams record.")]
		[Alias("r")]
		public async Task GetRecord(
			[Summary("The (optional) team to get info for")] string team = null)
		{
			Team t = TeamResolver.ResolveTeam(team, Context.User);

			StringBuilder sb = new();
			sb.AppendLine($"{t.Name}'s Total Stats: {t.Stats.ToString(true)}");
			foreach (KeyValuePair<string, TeamStatistics> stats in t.StageStats)
			{
				sb.AppendLine($"{stats.Key} Stats: {stats.Value.ToString(true)}");
			}

			await ReplyAsync(sb.ToString());
		}

		[Command("round")]
		[Summary("Returns info about a round.")]
		public async Task GetRound(
			[Summary("The (optional) round to get info for")] int? round = null)
		{
			List<BracketRound> rounds = LeagueManager.League.Bracket.Rounds;
			int roundIndex = round ?? rounds.Count - 1;
			BracketRound r = rounds[roundIndex];

			StringBuilder sb = new();
			sb.AppendLine($"{r.RoundName}");
			foreach (MatchResult match in r.Matches)
			{
				sb.AppendLine($"[{match.HomeGamesWon}-{match.AwayGamesWon}] (**{match.HomeTeam.Rank}**){match.HomeTeam} vs (**{match.AwayTeam.Rank}**){match.AwayTeam}");
			}

			await ReplyAsync(sb.ToString());
		}

		[Command("rematches")]
		[Summary("Gets the matches in the current round, which are a rematch of the current stage.")]
		public async Task GetRematches()
		{
			Bracket bracket = LeagueManager.League.Bracket;
			string stage = StageMatcher.Lookup(bracket.Rounds.Last().RoundName);
			string rematches = StageRematchFinder.FindRematches(bracket, stage);

			await ReplyAsync(rematches);
		}

		[Command("history")]
		[Summary("Returns info about your teams record.")]
		[Alias("h")]
		public async Task GetHistory(
			[Summary("The (optional) team to get info for")] string team = null)
		{
			League league = LeagueManager.League;

			Team t = TeamResolver.ResolveTeam(team, Context.User);

			StringBuilder sb = new();

			foreach (BracketRound round in league.Bracket.Rounds)
			{
				sb.Append($"{round.RoundName}: ");
				foreach (MatchResult result in round.Matches)
				{
					if (result.HomeTeam == t || result.AwayTeam == t)
					{
						sb.AppendLine($"[{result.HomeGamesWon}-{result.AwayGamesWon}] {result.HomeTeam} vs {result.AwayTeam}");
					}
				}
			}

			await ReplyAsync(sb.ToString());
		}

		[Command("team")]
		[Summary("Returns info about a team, including the players.")]
		[Alias("t")]
		public async Task GetTeam(
			[Summary("The (optional) team to get info for")] string team = null)
		{
			Team t = TeamResolver.ResolveTeam(team, Context.User);

			StringBuilder sb = new();

			sb.AppendLine($"Team: {t.Name}, Current Rank: {t.Rank} [{t.Stats.MatchWins}-{t.Stats.MatchLosses}]");
			sb.AppendLine($"Goal Differential: {t.Stats.TotalGoalDifferential}, Goals/Game: {(double)t.Stats.TotalGoals / t.Stats.TotalGames}");
			foreach (Player p in t.Players)
			{
				string captainTag = p.Captain ? "(c) " : "";
				sb.AppendLine($"{captainTag} {p.DiscordId}");
			}

			await ReplyAsync(sb.ToString());
		}

		[Command("forcerefresh")]
		[Summary("Forces new data from CEA site.")]
		public async Task ForceRefresh()
		{
			LeagueManager.ForceUpdate();

			await ReplyAsync("Refresh Completed.");
		}
	}
}
