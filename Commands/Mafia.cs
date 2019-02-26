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
            // Validate that we have more than zero mafia
            if (numMafias <= 0)
            {
                await Context.Channel.SendMessageAsync("Number must be positive dipstick!");
                return;
            }

            var mentions = Context.Message.MentionedUserIds;
            // Validate that more than one users were mentioned
            if (mentions.Count <= 1)
            {
                await Context.Channel.SendMessageAsync("You need more than 1 person to play! Mention some friends! You have friends don't you?");
                return;
            }
            // validate that number of mafia is less than number of players
            if (numMafias > mentions.Count)
            {
                await Context.Channel.SendMessageAsync("Number of mafia can not exceed players moron!");
                return;
            }

            var shuffled = mentions.AsEnumerable().Shuffle(); // shuffle teams
            var team1Size = mentions.Count / 2; // round down if odd

            var mafia = shuffled.Shuffle().Take(numMafias); // shuffle again and pick mafia

            // separate teams
            var team1 = shuffled.Take(team1Size);
            var team2 = shuffled.Skip(team1Size);

            // Send messages to mafia
            foreach(var u in mafia)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await user.SendMessageAsync("You are in the Mafia!");
            }

            // Send messages to Team 1
            string msg = "Team1: ";
            foreach (var u in team1)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await user.SendMessageAsync("You are to fight on Team 1!");
                msg += $"{user.Mention} ";
            }

            // Send messages to team 2
            msg += "Team2: ";
            foreach (var u in team2)
            {
                var user = await Context.Guild.GetUserAsync(u);
                await user.SendMessageAsync("You are to fight on Team 2!");
                msg += $"{user.Mention} ";
            }

            // Send message to channel specifying teams
            await Context.Channel.SendMessageAsync(msg);
        }

        
    }
}
