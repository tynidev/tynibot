using Discord;
using LiteDB;
using System;
using System.Threading.Tasks;

namespace TyniBot.Models
{
    public class MafiaPlayer : IUser
    {
        #region IUser Members
        [BsonIgnore]
        public IUser DiscordUser { get; set; }

        [BsonIgnore]
        public string AvatarId => DiscordUser.AvatarId;

        [BsonIgnore]
        public string Discriminator => DiscordUser.Discriminator;

        [BsonIgnore]
        public ushort DiscriminatorValue => DiscordUser.DiscriminatorValue;

        [BsonIgnore]
        public bool IsBot => DiscordUser.IsBot;

        [BsonIgnore]
        public bool IsWebhook => DiscordUser.IsWebhook;

        public string Username => DiscordUser.Username == null ? string.Empty : DiscordUser.Username;

        [BsonIgnore]
        public DateTimeOffset CreatedAt => DiscordUser.CreatedAt;

        [BsonIgnore]
        public string Mention => DiscordUser.Mention;

        [BsonIgnore]
        public IActivity Activity => DiscordUser.Activity;

        [BsonIgnore]
        public UserStatus Status => DiscordUser.Status;

        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            return DiscordUser.GetAvatarUrl(format, size);
        }

        public string GetDefaultAvatarUrl()
        {
            return DiscordUser.GetDefaultAvatarUrl();
        }

        public Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null)
        {
            return DiscordUser.GetOrCreateDMChannelAsync(options);
        }
        #endregion

        #region Mafia Members

        [BsonId]
        public ulong Id { get; set; }
        public bool IsMafia { get; set; }
        public bool OnTeam1 { get; set; }
        public bool OnTeam2 { get; set; }
        #endregion

        public override string ToString()
        {
            return $"{Id}:{Username}";
        }
    }
}
