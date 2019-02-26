using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace TyniBot
{
    public class Mafia : ModuleBase
    {
        [Command("mafia"), Summary("Picks the mafia!")]
        public async Task mafiagame(int numMafias, [Remainder]string message = "")
        {
            if (numMafias < 0)
            {
                await Context.Channel.SendMessageAsync("Number of mafia can not be negative dipstick!");
                return;
            }

            var mentions = Context.Message.MentionedUserIds;
            if (numMafias > mentions.Count)
            {
                await Context.Channel.SendMessageAsync("Number of mafia can not exceed mentions moron!");
                return;
            }

            var shuffled = mentions.AsEnumerable().Shuffle();
            var team1Size = mentions.Count / 2;

            var mafia = shuffled.Take(numMafias);
            var team1 = mentions.Take(team1Size);
            var team2 = mentions.Skip(team1Size);

            foreach(var u in mafia)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await Discord.UserExtensions.SendMessageAsync(user, "You are in the Mafia!");
            }

            string msg = "Team1: ";
            foreach (var u in team1)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await Discord.UserExtensions.SendMessageAsync(user, "You are to fight on Team 1!");
                msg += $"{user.Mention} ";
            }

            msg += "Team2: ";
            foreach (var u in team2)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await Discord.UserExtensions.SendMessageAsync(user, "You are to fight on Team 2!");
                msg += $"{user.Mention} ";
            }

            await Context.Channel.SendMessageAsync(msg);
        }

        
    }
}
