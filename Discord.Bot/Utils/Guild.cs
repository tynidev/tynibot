using Azure;
using Azure.Data.Tables;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Bot.Utils
{
    public class Guild
    {
        public static string PartitionKeyConst = "guilds";
        public static string TableName = "recruitingguilds";
        
        public ulong Id { get; set; }
        public ulong RecruitingChannelId { get; set; }
        public ulong RolesChannelId { get; set; }
        public ulong InhouseChannelId { get; set; }
        public List<ApplicationCommandPermission> AdminRoles { get; set; }
        public List<ApplicationCommandPermission> PlayerRoles { get; set; }

        public string RowKey => this.Id.ToString();

        public static async Task<Guild> GetGuildAsync(ulong id, StorageClient storageClient)
        {
            // might want to cache these guilds eventually
            var guild = await storageClient.GetTableRow<Guild>(Guild.TableName, id.ToString(), Guild.PartitionKeyConst);

            if (guild == null)
            {
                GuildIdMappings.recruitingChannels.TryGetValue(id, out ulong recruitingChannelId);
                GuildIdMappings.inhouseChannels.TryGetValue(id, out ulong inhouseChannelId);
                GuildIdMappings.rolesChannels.TryGetValue(id, out ulong rolesChannelId);
                guild = new Guild
                {
                    Id = id,
                    RecruitingChannelId = recruitingChannelId,
                    InhouseChannelId = inhouseChannelId,
                    RolesChannelId = rolesChannelId
                };

                await storageClient.SaveTableRow(TableName, guild.RowKey, PartitionKeyConst, guild);
            }

            return guild;
        }
    }
}
