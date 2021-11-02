using Discord;
using Discord.Bot;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot.Recruiting;

namespace TyniBot.Commands
{
    public abstract class RecruitingCommand : SlashCommand
    {
        public override bool DefaultPermissions => false;

        public override bool IsGlobal => false;

        protected static readonly ImmutableDictionary<ulong, ulong> recruitingChannelForGuild = new Dictionary<ulong, ulong> {
            { 902581441727197195, 903521423522398278}, //tynibot test
            { 598569589512863764,  541894310258278400}, //msft rl
            { 801598108467200031,  904856579403300905}, //tyni's server
            { 904804698484260874, 904867794376618005 } // nates server
        }.ToImmutableDictionary();

        internal List<Team> ParseMessageAsync(List<IMessage> messages)
        {
            var teams = new List<Team>();

            foreach (var teamMsg in messages)
            {
                teams.Add(Team.ParseTeam(teamMsg.Id, teamMsg.Content));
            }

            return teams;
        }

        internal async Task<List<IMessage>> GetAllChannelMessages(ISocketMessageChannel channel, int limit = 100)
        {
            var msgs = new List<IMessage>();
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            while (messages.Count() > 0 && msgs.Count() < limit)
            {
                msgs.AddRange(messages);
                messages = await channel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();
            }

            return msgs;
        }
    }
}
