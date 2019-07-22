using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LiteDB;
using TyniBot;
using Discord.WebSocket;

namespace Discord.Inhouse
{
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

        Dictionary<string, Mode> ModeMap = new Dictionary<string, Mode>()
        {
            { "3", Mode.Standard },
            { "2", Mode.Doubles },
            { "1", Mode.Duel },
        };

        #region Commands
        [Command("inhouse"), Summary("**!inhouse <rank=(c1,d2,p3 etc....)>** Creates a new game of inhouse soccar! Each individual player needs to join.")]
        public async Task NewInhouseCommand(string rank)
        {
            try
            {
                int mmr = (int)ParseRank(rank);
                var owner = Player.ToPlayer(Context.User, mmr);
                var queue = await CreateQueue(owner);
                await Output.QueueStarted(Context.Channel, queue);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("join"), Summary("**!join <rank=(c1,d2,p3 etc....)>** Joins a new game of inhouse soccar!")]
        public async Task JoinCommand(string rank)
        {
            try
            {
                int mmr = (int)ParseRank(rank);
                var player = Player.ToPlayer(Context.User, mmr);
                var queue = await QueuePlayer(player);
                await Output.PlayersAdded(Context.Channel, queue, new List<Player>() { player });
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("leave"), Summary("**!leave** Leaves a new game of inhouse soccar!")]
        public async Task LeaveCommand()
        {
            try
            {
                var player = Player.ToPlayer(Context.User, 0);
                var players = new List<Player>() { player };
                var queue = await DequeuePlayers(players);
                await Output.PlayersRemoved(Context.Channel, queue, players);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("boot"), Summary("**!boot <@player>** Kicks a player from the queue for inhouse soccar!")]
        public async Task BootCommand([Remainder]string message = "")
        {
            try
            {
                var players = Context.Message.MentionedUsers.Select(s => Player.ToPlayer(s, 0)).ToList();
                var queue = await DequeuePlayers(players);
                await Output.PlayersRemoved(Context.Channel, queue, players);
            }
            catch(Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("teams"), Summary("**!teams <mode=(3,2,1)>** Divides teams \"equally\"!")]
        public async Task TeamsCommand(string modeStr)
        {
            try
            {
                Mode mode = ParseMode(modeStr);
                object output = await DivideTeams(mode);
                // TODO: Output something
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync($"Error: {e.Message}");
            }
        }

        [Command("inhouse"), Summary("**!inhouse help** | Displays this help text.")]
        public async Task HelpCommand()
        {
            await Output.HelpText(Context.Channel);
        }
        #endregion

        #region Helpers

        private Mode ParseMode(string mode)
        {
            if (!ModeMap.ContainsKey(mode.ToLower()))
                throw new ArgumentException($"Unsupported Mode({mode})");

            return ModeMap[mode];
        }

        private Rank ParseRank(string rank)
        {
            if (!RankMap.ContainsKey(rank.ToLower()))
                throw new ArgumentException($"Unsupported Rank({rank})");

            return RankMap[rank];
        }

        private async Task<InhouseQueue> CreateQueue(Player owner)
        {
            var newQueue = new InhouseQueue(Context.Channel.Id, owner);

            var queues = Context.Database.GetCollection<InhouseQueue>();

            // Delete current queue if exists
            try
            {
                var existing = await InhouseQueue.GetQueueAsync(Context.Channel.Id, Context.Client, queues);
                if (existing != null)
                    queues.Delete(g => g.Id == existing.Id);
            }
            catch (Exception) { }

            // Insert into DB
            queues.Insert(newQueue);
            queues.EnsureIndex(x => x.Id);

            return newQueue;
        }

        private async Task<InhouseQueue> QueuePlayer(Player player)
        {
            var queues = Context.Database.GetCollection<InhouseQueue>();
            var queue = await InhouseQueue.GetQueueAsync(Context.Channel.Id, Context.Client, queues);
            if (queue == null) throw new ArgumentException("Did not find any current inhouse queue for this channel.");

            if (queue.Players.ContainsKey(player.Id))
            {   // update player if already exists to allow MMR updates
                queue.Players[player.Id] = player;
            }
            else
            {   // else just queue the new player
                queue.Players.Add(player.Id, player);
            }

            queues.Update(queue);
            return queue;
        }

        private async Task<InhouseQueue> DequeuePlayers(List<Player> players)
        {
            var queues = Context.Database.GetCollection<InhouseQueue>();
            var queue = await InhouseQueue.GetQueueAsync(Context.Channel.Id, Context.Client, queues);
            if (queue == null) throw new ArgumentException("Did not find any current inhouse queue for this channel.");

            foreach (var player in players)
            {
                if (queue.Players.ContainsKey(player.Id))
                {
                    queue.Players.Remove(player.Id);
                }
            }

            queues.Update(queue);
            return queue;
        }

        private async Task<object> DivideTeams(Mode mode)
        {
            var queues = Context.Database.GetCollection<InhouseQueue>();
            var queue = await InhouseQueue.GetQueueAsync(Context.Channel.Id, Context.Client, queues);
            if (queue == null) throw new ArgumentException("Did not find any current inhouse queue for this channel.");

            // TODO: Calculate possible teams with queue.Players and Mode and return the possibilities 
            return null;
        }

        #endregion
    }
}
