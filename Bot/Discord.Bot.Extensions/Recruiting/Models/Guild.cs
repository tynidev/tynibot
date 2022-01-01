using Azure;
using Azure.Data.Tables;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Recruiting
{
    public class Guild
    {
        public static string PartitionKeyConst = "guilds";
        public static string TableName = "recruitingguilds";
        
        public string Id { get; set; }
        public ulong ChannelId { get; set; }
        public List<ApplicationCommandPermission> AdminRoles { get; set; }
        public List<ApplicationCommandPermission> PlayerRoles { get; set; }
    }
}
