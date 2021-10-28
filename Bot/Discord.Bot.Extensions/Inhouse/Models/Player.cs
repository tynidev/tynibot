﻿using LiteDB;
using System;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Discord.Inhouse
{
    public class Player : IUser
    { 
        #region Mafia Members
        [BsonId]
        public ulong Id { get; set; }

        private string m_userName;
        public string Username
        {
            get
            {
                if (DiscordUser == null && m_userName == null)
                {
                    return string.Empty;
                }
                else if (DiscordUser != null)
                {
                    return DiscordUser.Username;
                }
                else
                {
                    return m_userName;
                }
            }

            set
            {
                m_userName = value;
            }
        }

        public int MMR { get; set; } = 0;
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
        public IImmutableList<IActivity> Activities => DiscordUser.Activities;

        [BsonIgnore]
        public UserStatus Status => DiscordUser.Status;

        [BsonIgnore]
        public UserProperties? PublicFlags => DiscordUser.PublicFlags;

        [BsonIgnore]
        public IImmutableSet<ClientType> ActiveClients => DiscordUser.ActiveClients;

        [BsonIgnore]
        public string BannerId => DiscordUser.BannerId;

        [BsonIgnore]
        public Color? AccentColor => DiscordUser.AccentColor;

        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            return DiscordUser.GetAvatarUrl(format, size);
        }

        public string GetDefaultAvatarUrl()
        {
            return DiscordUser.GetDefaultAvatarUrl();
        }

        public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
        {
            return DiscordUser.CreateDMChannelAsync(options);
        }

        public string GetBannerUrl(ImageFormat format = ImageFormat.Auto, ushort size = 256)
        {
            return DiscordUser.GetBannerUrl(format, size);
        }
        #endregion

        public override string ToString()
        {
            return $"{Id}:{Username}";
        }

        public static Player ToPlayer(IUser user, int mmr)
        {
            return new Player()
            {
                Id = user.Id,
                DiscordUser = user,
                MMR = mmr
            };
        }        
    }
}
