using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LiteDB;
using TyniBot;
using Discord.WebSocket;
using Discord.Matches;

namespace Discord.Inhouse
{
    [Group("inhouse")]
    public class InhouseCommand : ModuleBase<TyniBot.CommandContext>
    {
        Dictionary<string, Rank> RankMap = new Dictionary<string, Rank>()
        {
            { "gc", Rank.GrandChamp },
            { "c3", Rank.Champ3 },
            { "c2", Rank.Champ2 },
            { "c1", Rank.Champ1 },
            { "d3", Rank.Diamond3 },
            { "d2", Rank.Diamond2 },
            { "d1", Rank.Diamond1 },
            { "p3", Rank.Plat3 },
            { "p2", Rank.Plat2 },
            { "p1", Rank.Plat1 },
            { "g3", Rank.Gold3 },
            { "g2", Rank.Gold2 },
            { "g1", Rank.Gold1 },
            { "s3", Rank.Silver3 },
            { "s2", Rank.Silver2 },
            { "s1", Rank.Silver1 },
            { "b3", Rank.Bronze3 },
            { "b2", Rank.Bronze2 },
            { "b1", Rank.Bronze1 }
        };

        Dictionary<string, TeamSize> TeamSizeMap = new Dictionary<string, TeamSize>()
        {
            { "3", TeamSize.Standard },
            { "2", TeamSize.Doubles },
            { "1", TeamSize.Duel },
        };

        Dictionary<string, SplitMode> SplitModeMap = new Dictionary<string, SplitMode>()
        {
            { "random", SplitMode.Random},
            { "skillgroup", SplitMode.SkillGroup},
        };

        Dictionary<int, List<int>> PlayerMatchSplits = new Dictionary<int, List<int>>()
        {
            { 1, new List<int>(){ 1 } },
            { 2, new List<int>(){ 2 } },
            { 3, new List<int>(){ 3 } },
            { 4, new List<int>(){ 4 } },
            { 5, new List<int>(){ 5 } },
            { 6, new List<int>(){ 6 } },
            { 7, new List<int>(){ 7 } },
            { 8, new List<int>(){ 4, 4 } },
            { 9, new List<int>(){ 5, 4 } },
            { 10, new List<int>(){ 6, 4 } },
            { 11, new List<int>(){ 7, 4 } },
        };

        #region Commands
        [Command("new"), Summary("**!inhouse new <queueName>** Creates a new game of inhouse soccar! Each individual player needs to join.")]
        [Alias("newQueue", "makeQueue", "newqueue", "makequeue")]
        public async Task NewInhouseCommand(string name)
        {
            try
            {
                var queue = await CreateQueue(name);
                await Output.QueueStarted(Context.Channel, queue);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("join"), Summary("**!inhouse join <queueName> <rank=(c1,d2,p3 etc....)>** Joins a new game of inhouse soccar!")]
        [Alias("queue")]
        public async Task JoinCommand(string queueOrRank, [Remainder]string maybeRank = "")
        {
            try
            {
                int mmr = 0;
                string queueName = queueOrRank;
                var queues = Context.Database.GetCollection<InhouseQueue>();
                var localQueues = queues.Find(q => q.ChannelId == Context.Channel.Id);
                

                if ( localQueues.Count() == 1)
                {
                    try
                    {
                        mmr = (int)ParseRank(queueOrRank);
                        queueName = localQueues.First().Name;
                    }
                    catch (ArgumentException){}
                }

                if (mmr == 0)
                {
                    mmr = (int)ParseRank(maybeRank);
                }

                var player = Player.ToPlayer(Context.User, mmr);
                var queue = await QueuePlayer(queueName, player);
                await Output.PlayersAdded(Context.Channel, queue, new List<Player>() { player });
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("leave"), Summary("**!inhouse leave <queueName>** Leaves a new game of inhouse soccar!")]
        [Alias("drop")]
        public async Task LeaveCommand([Remainder]string queueName ="")
        {
            try
            {
                var player = Player.ToPlayer(Context.User, 0);
                var players = new List<Player>() { player };

                if (queueName == "")
                {
                    var queues = Context.Database.GetCollection<InhouseQueue>();
                    var localQueues = queues.Find(q => q.ChannelId == Context.Channel.Id);
                    int queuesLeft = 0;
                    string letterS = "s";

                    if (queuesLeft == 1)
                    {
                        letterS = "";
                    }

                    foreach (InhouseQueue inhouseQueue in localQueues)
                    {
                        if (inhouseQueue.Players.ContainsKey(player.Id))
                        {
                            inhouseQueue.Players.Remove(player.Id);
                            queuesLeft++;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"Successfully left {queuesLeft} queue{letterS}");
                }
                else
                {
                    var queue = await DequeuePlayers(queueName, players);
                    await Output.PlayersRemoved(Context.Channel, queue, players);
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("kick"), Summary("**!inhouse kick <queueName> <@player>** Kicks a player from the queue for inhouse soccar!")]
        [Alias("boot")]
        public async Task BootCommand(string queueName, [Remainder]string message = "")
        {
            try
            {
                var players = Context.Message.MentionedUsers.Select(s => Player.ToPlayer(s, 0)).ToList();
                var queue = await DequeuePlayers(queueName, players);
                await Output.PlayersRemoved(Context.Channel, queue, players);
            }
            catch(Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("close"), Summary("**!inhouse close <queueName>** Kills a queue for inhouse soccar!")]
        [Alias ("delete", "kill")]
        public async Task CloseCommand(string queueName, [Remainder]string message = "")
        {
            try
            {
                await DeleteQueue(queueName);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("closeall"), Summary("**!inhouse closeall ** Kills all queues for inhouse soccar in this channel!")]
        [Alias("killall", "deleteall")]
        public async Task CloseAllCommand([Remainder]string message = "")
        {
            try
            {
                var queues = Context.Database.GetCollection<InhouseQueue>();
                int numDeleted = queues.Delete(g => g.ChannelId == Context.Channel.Id);

                string letterS = "s";

                if (numDeleted == 1)
                {
                    letterS = "";
                }

                await Context.Channel.SendMessageAsync($"Deleted {numDeleted} queue" + letterS + " from this channel.");

            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("listqueues"), Summary("**!inhouse listqueues ** List all queues for inhouse soccar in this channel!")]
        [Alias("listall")]
        public async Task ListQueuesCommand([Remainder]string message = "")
        {
            try
            {
                var queues = Context.Database.GetCollection<InhouseQueue>();

                var channelQueues = queues.Find(g => g.ChannelId == Context.Channel.Id);

                if (channelQueues.Count() > 0)
                {
                    string queuesString = "";
                    foreach (InhouseQueue queue in channelQueues)
                    {
                        queuesString += queue.Name + "\r\n";
                    }

                    await Context.Channel.SendMessageAsync($"Active queues: \r\n{queuesString}");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"No active queues running in this channel.");
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("teams"), Summary("**!inhouse teams <queueName> <mode=(3,2,1)> <splitMode=(random, skillgroup)>** Divides teams \"equally\"!")]
        [Alias("maketeams")]
        public async Task TeamsCommand(string queueName, string teamSizeStr, string splitModeStr)
        {
            try
            {
                TeamSize size = ParseTeamSize(teamSizeStr);
                SplitMode splitMode = ParseSplitMode(splitModeStr);

                var queue = await GetQueue(queueName);

                if (queue == null)
                {
                    throw new ArgumentException("Did not find any current inhouse queue for this channel.");
                }

                List<List<Player>> playerGroups = SplitQueue(size, queue, splitMode);
                int groupNumber = 1;

                foreach (List<Player> players in playerGroups)
                {
                    var matches = DivideTeams(players);

                    if (matches != null)
                    {
                        await Output.OutputUniqueMatches(matches, groupNumber, Context.Channel);
                    }

                    ++groupNumber;
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("faketeams"), Summary("**!inhouse faketeams <queueName> <players>** Fills up the queue with enough fake players at random ranks, up to the number of players requested.")]
        [Alias("fakeTeams")]
        public async Task FakeTeamsCommand(string queueName, string playersCount)
        {
            try
            {
                int numPlayers;
                Int32.TryParse(playersCount, out numPlayers);
                var queues = Context.Database.GetCollection<InhouseQueue>();
                var queue = await GetQueue(queueName);

                if (numPlayers <= queue.Players.Count)
                {
                    await Context.Channel.SendMessageAsync($"Already enough players!");
                    return;
                }

                Random rnd = new Random();

                for (int i=0; i < numPlayers - queue.Players.Count; i++)
                {
                    Player botPlayer = new Player();
                    botPlayer.Id = (ulong)i;
                    botPlayer.Username = "[MSFT] FizzBuzz " + i.ToString();
                    botPlayer.MMR = (int)RankMap.Values.ElementAt<Rank>(rnd.Next(1, RankMap.Values.Count));

                    await QueuePlayer(queueName, botPlayer);
                   
                }

            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("help"), Summary("**!inhouse help** | Displays this help text.")]
        public async Task HelpCommand()
        {
            await Output.HelpText(Context.Channel);
        }

        [Command(""), Summary("**!inhouse** | Displays this help text.")]
        public async Task CatchAllCommand()
        {
            await Output.HelpText(Context.Channel);
        }
        #endregion

        #region Helpers

        private TeamSize ParseTeamSize(string teamSize)
        {
            if (!TeamSizeMap.ContainsKey(teamSize.ToLower()))
                throw new ArgumentException($"Unsupported Mode({teamSize})");

            return TeamSizeMap[teamSize];
        }

        private SplitMode ParseSplitMode(string splitMode)
        {
            if (!SplitModeMap.ContainsKey(splitMode.ToLower()))
            {
                throw new ArgumentException($"Unsupported SplitMode({splitMode})");
            }

            return SplitModeMap[splitMode];
        }

        private Rank ParseRank(string rank)
        {
            if (!RankMap.ContainsKey(rank.ToLower()))
                throw new ArgumentException($"Unsupported Rank({rank})");

            return RankMap[rank];
        }

        private async Task<InhouseQueue> CreateQueue(string queueName)
        {
            var newQueue = new InhouseQueue(Context.Channel.Id, queueName);

            var queues = Context.Database.GetCollection<InhouseQueue>();

            // Delete current queue if exists
            try
            {
                var existing = await InhouseQueue.GetQueueAsync(Context.Channel.Id, queueName, Context.Client, queues);
                if (existing != null)
                    queues.Delete(g => g.Name == existing.Name);
            }
            catch (Exception) { }

            // Insert into DB
            queues.Insert(newQueue);
            queues.EnsureIndex(x => x.Name);

            return newQueue;
        }

        private async Task DeleteQueue(string queueName)
        {
            var queues = Context.Database.GetCollection<InhouseQueue>();

            var existing = await InhouseQueue.GetQueueAsync(Context.Channel.Id, queueName, Context.Client, queues);
            if (existing != null)
            {
                queues.Delete(g => g.Name == existing.Name);
                await Context.Channel.SendMessageAsync($"Queue {queueName} deleted.");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Queue {queueName} not found in this channel.");
            }
        }

        private async Task<InhouseQueue> QueuePlayer(string queueName, Player player)
        {
            var queue = await GetQueue(queueName);

            if (queue.Players.ContainsKey(player.Id))
            {   // update player if already exists to allow MMR updates
                queue.Players[player.Id] = player;
            }
            else
            {   // else just queue the new player
                queue.Players.Add(player.Id, player);
            }

            var queues = Context.Database.GetCollection<InhouseQueue>();
            queues.Update(queue);
            return queue;
        }

        private async Task<InhouseQueue> DequeuePlayers(string queueName, List<Player> players)
        {
            var queue = await GetQueue(queueName);

            foreach (var player in players)
            {
                if (queue.Players.ContainsKey(player.Id))
                {
                    queue.Players.Remove(player.Id);
                }
            }

            var queues = Context.Database.GetCollection<InhouseQueue>();
            queues.Update(queue);
            return queue;
        }

        private List<Tuple<List<Player>, List<Player>>> DivideTeams(List<Player> players)
        {
            int teamSize = players.Count / 2;
            int remainder = players.Count % 2;

            var uniqueTeams = Combinations.Combine<Player>(players, minimumItems: teamSize, maximumItems: teamSize+remainder);
            var matches = new List<Tuple<List<Player>, List<Player>>>();

            while (uniqueTeams.Count > 0)
            {
                var team1 = uniqueTeams.First();
                var team2 = uniqueTeams.Where(l => {
                    if (remainder == 0 || l.Count > teamSize)
                    {
                        return l.ContainsNone(team1);
                    }
                    else
                    {
                        return l.Count < team1.Count && team1.ContainsNone(l);
                    }
                }
                ).First();

                matches.Add(new Tuple<List<Player>, List<Player>>(team1, team2));

                uniqueTeams.Remove(team1);
                uniqueTeams.Remove(team2);
            }
            TeamComparer teamComparer = new TeamComparer();
            matches.Sort(teamComparer);

            // TODO: Calculate possible teams with queue.Players and TeamSize and return the possibilities 
            return matches;
        }

        private List<List<Player>> SplitQueue(TeamSize sizeHint, InhouseQueue queue, SplitMode splitMode)
        {
            if (queue == null)
            {
                return null;
            }

            List<Player> players = queue.Players.Values.ToList<Player>();

            if (splitMode == SplitMode.Random)
            {
                return SplitQueueRandom(players, sizeHint);
            }
            else if (splitMode == SplitMode.SkillGroup)
            {
                return SplitQueueSkillGroup(players, sizeHint);
            }
            else
            {
                return null;
            }
        }

        private List<List<Player>> SplitQueueSkillGroup(List<Player> players, TeamSize sizeHint)
        {
            PlayerMMRComparer mmrComparer = new PlayerMMRComparer();
            players.Sort(mmrComparer);
            return SplitSortedGroup(players, sizeHint);
        }

        private List<List<Player>> SplitQueueRandom(List<Player> players, TeamSize sizeHint)
        {
            Random rnd = new Random();
            
            return SplitSortedGroup(players.OrderBy(x => rnd.Next()).ToList(), sizeHint);
        }

        private List<List<Player>> SplitSortedGroup(List<Player> players, TeamSize sizeHint)
        {
            int matchSize = (int)sizeHint * 2;

            List<List<Player>> playerGroups = new List<List<Player>>();

            while (players.Count > PlayerMatchSplits.Count)
            {
                playerGroups.Add(players.Take(matchSize).ToList());
                players.RemoveRange(0, matchSize);
            }

            if (players.Count > 0)
            {
                foreach(int playerCount in PlayerMatchSplits[players.Count])
                {
                    playerGroups.Add(players.Take(playerCount).ToList());
                    players.RemoveRange(0, playerCount);
                }
            }

            if (players.Count > 0)
            {
                throw new ArgumentException($"We didn't handle the last {players.Count} players when splitting!");
            }

            return playerGroups;
        }

        private async Task<InhouseQueue> GetQueue(string queueName)
        {
            var queues = Context.Database.GetCollection<InhouseQueue>();
            var queue = await InhouseQueue.GetQueueAsync(Context.Channel.Id, queueName, Context.Client, queues);
            if (queue == null) throw new ArgumentException("Did not find any current inhouse queue for this channel.");
            return queue;
        }

        #endregion
    }
}
