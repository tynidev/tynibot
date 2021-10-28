using LiteDB;
using System;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Discord.Mafia
{
    public enum PlayerType
    {
        Villager,
        Mafia,
        Joker
    }

    public enum Team
    {
        Orange,
        Blue
    }

    public class Player : IUser
    {
        #region Mafia Members
        [BsonId]
        public ulong Id { get; set; }
        public PlayerType Type { get; set; }
        public Team Team { get; set; }
        public string Username => DiscordUser?.Username == null ? string.Empty : DiscordUser.Username;
        public string Emoji { get; set; }
        public int Score { get; set; } = 0;
        #endregion

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

        [BsonIgnore]
        public DateTimeOffset CreatedAt => DiscordUser.CreatedAt;

        [BsonIgnore]
        public string Mention => DiscordUser.Mention;

        [BsonIgnore]
        public UserStatus Status => DiscordUser.Status;

        [BsonIgnore]
        public UserProperties? PublicFlags => DiscordUser.PublicFlags;

        [BsonIgnore]
        public IImmutableSet<ClientType> ActiveClients => DiscordUser.ActiveClients;

        [BsonIgnore]
        public IImmutableList<IActivity> Activities => DiscordUser.Activities;

        [BsonIgnore]
        public string BannerId => DiscordUser.BannerId;

        [BsonIgnore]
        public Color? AccentColor => DiscordUser.AccentColor;

        public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
        {
            return DiscordUser.CreateDMChannelAsync(options);
        }

        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            return DiscordUser.GetAvatarUrl(format, size);
        }

        public string GetBannerUrl(ImageFormat format = ImageFormat.Auto, ushort size = 256)
        {
            return DiscordUser.GetBannerUrl(format, size);
        }

        public string GetDefaultAvatarUrl()
        {
            return DiscordUser.GetDefaultAvatarUrl();
        }
        #endregion

        public override string ToString()
        {
            return $"{Id}:{Username}";
        }
    }
}
