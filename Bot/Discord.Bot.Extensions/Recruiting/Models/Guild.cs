using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Recruiting
{
    public class Guild : ITableEntity
    {
        public static string PartitionKeyConst = "guilds";
        public static string TableName = "recruitingguilds";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public ulong ChannelId { get; set; }
        public string PartitionKey { get; set; }
        public List<ulong> AdminRoles { get; set; }
        public List<ulong> PlayerRoles { get; set; }
    }
}
