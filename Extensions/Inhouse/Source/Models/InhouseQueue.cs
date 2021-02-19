using Discord;
using System;
using LiteDB;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Inhouse
{
    public enum Rank
    {
        SuperSonicLegend = 1880,
        GrandChamp3 = 1770,
        GrandChamp2 = 1655,
        GrandChamp1 = 1535,
        Champ3 = 1435,
        Champ2 = 1335,
        Champ1 = 1235,
        Diamond3 = 1135,
        Diamond2 = 1035,
        Diamond1 = 935,
        Plat3 = 855,
        Plat2 = 775,
        Plat1 = 695,
        Gold3 = 610,
        Gold2 = 550,
        Gold1 = 490,
        Silver3 = 430,
        Silver2 = 370,
        Silver1 = 315,
        Bronze3 = 250,
        Bronze2 = 190,
        Bronze1 = 0
    }

    public enum TeamSize
    {
        Duel = 1, 
        Doubles = 2,
        Standard = 3
    }

    public class InhouseQueue
    {
        public ulong ChannelId { get; set; }

        [BsonId]
        public string Name { get; set; }

        public Dictionary<ulong, Player> Players { get; private set; } = new Dictionary<ulong, Player>();

        public InhouseQueue() { }

        public InhouseQueue(ulong channelId, string name)
        {
            ChannelId = channelId;
            Name = name;
            Players = new Dictionary<ulong, Player>();
        }
        public static async Task<InhouseQueue> GetQueueAsync(ulong channelId, string name, IDiscordClient channel, ILiteCollection<InhouseQueue> collection)
        {
            var queue = collection.FindOne(g => g.ChannelId == channelId && g.Name == name);
            if (queue == null)
                return null;

            foreach (var u in queue.Players.Values)
                u.DiscordUser = await channel.GetUserAsync(u.Id);

            return queue;
        }
    }
}
