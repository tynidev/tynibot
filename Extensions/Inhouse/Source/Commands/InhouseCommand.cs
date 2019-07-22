using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LiteDB;
using TyniBot;
using Discord.WebSocket;

namespace Discord.Inhouse
{
    public class InhouseCommand : ModuleBase<TyniBot.CommandContext>
    {
        public enum Rank
        {
            GrandChamp = 1500,
            Champ3 = 1400,
            Champ2 = 1300,
            Champ1 = 1200,
            Diamond3 = 1100,
            Diamond2 = 1000,
            Diamond1 = 930,
            Plat3 = 855,
            Plat2 = 775,
            Plat1 = 700,
            Gold3 = 615,
            Gold2 = 555,
            Gold1 = 495,
            Silver3 = 435,
            Silver2 = 375,
            Silver1 = 315,
            Bronze3 = 255,
            Bronze2 = 195,
            Bronze1 = 60
        }

        public enum Mode
        {
            Standard,
            Doubles,
            Duel
        }

        #region Commands
        [Command("inhouse"), Summary("**!inhouse <rank=(c1,d2,p3 etc....)>** Creates a new game of inhouse soccar! Each individual player needs to join.")]
        public async Task NewInhouseCommand(string rank)
        {
            var owner = (IUser)Context.User;
            int mmr = 0;

            await CreateQueue(owner);

            await QueuePlayer(owner, mmr);
        }

        [Command("join"), Summary("**!join <rank=(c1,d2,p3 etc....)>** Joins a new game of inhouse soccar!")]
        public async Task JoinCommand(string rank)
        {
            int mmr = 0;
            var player = (IUser)Context.User;
            await QueuePlayer(player, mmr);
        }

        [Command("leave"), Summary("**!leave** Leaves a new game of inhouse soccar!")]
        public async Task LeaveCommand()
        {
            var player = (IUser)Context.User;
            await DequeuePlayer(player);
        }

        [Command("boot"), Summary("**!boot <@player>** Kicks a player from the queue for inhouse soccar!")]
        public async Task BootCommand([Remainder]string message = "")
        {
            var player = Context.Message.MentionedUsers.Select(s => (IUser)s).ToList().First();
            await DequeuePlayer(player);
        }

        [Command("teams"), Summary("**!teams <mode=(3,2,1)>** Divides teams \"equally\"!")]
        public async Task TeamsCommand(string mode)
        {
            await DivideTeams(mode);
        }

        [Command("inhouse"), Summary("**!inhouse help** | Displays this help text.")]
        public async Task HelpCommand()
        {
            await Output.HelpText(Context.Channel);
        }
        #endregion

        #region Helpers
        private Task CreateQueue(IUser owner)
        {
            throw new NotImplementedException();
        }

        private Task QueuePlayer(IUser owner, int mmr)
        {
            throw new NotImplementedException();
        }

        private Task DequeuePlayer(IUser player)
        {
            throw new NotImplementedException();
        }

        private Task DivideTeams(string mode)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
