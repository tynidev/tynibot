using Discord;
using LiteDB;
using System;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;

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

    public class Player
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

        [BsonIgnore]
        public IUser DiscordUser { get; set; }

        
        public override string ToString()
        {
            return $"{Id}:{Username}";
        }
    }
}
