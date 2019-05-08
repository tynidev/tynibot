using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TyniBot;
using EmojiData;

namespace Discord.Mafia
{
    public class Output
    {
        public static readonly string OrangeEmoji = EmojiLibrary.ByShortname(":large_orange_diamond:").Unicode;
        public static readonly string BlueEmoji = EmojiLibrary.ByShortname(":large_blue_diamond:").Unicode;
        public static readonly string OvertimeEmoji = EmojiLibrary.ByShortname(":alarm_clock:").Unicode;
        public static readonly string EndedEmoji = EmojiLibrary.ByShortname(":checkered_flag:").Unicode;

        private static List<string[]> PossibleEmjoiGroups = new List<string[]>()
        {
            new string[]{
                EmojiLibrary.ByShortname(":one:").Unicode,
                EmojiLibrary.ByShortname(":two:").Unicode,
                EmojiLibrary.ByShortname(":three:").Unicode,
                EmojiLibrary.ByShortname(":four:").Unicode,
                EmojiLibrary.ByShortname(":five:").Unicode,
                EmojiLibrary.ByShortname(":six:").Unicode,
                EmojiLibrary.ByShortname(":seven:").Unicode,
                EmojiLibrary.ByShortname(":eight:").Unicode,
            },
        };

        private static Random rand = new Random();
        private static string[] PossiblePlayerEmojis()
        {
            return PossibleEmjoiGroups[rand.Next(PossibleEmjoiGroups.Count)];
        }

        public static async Task<List<IUserMessage>> NotifyStartGame(Game game)
        {
            // Notify each Player
            var msgs = new List<IUserMessage>();
            foreach (var player in game.Players.Values)
                msgs.Add(await player.SendMessageAsync($"You are a {player.Type} on {player.Team} Team!"));
            return msgs;
        }

        public static async Task<IUserMessage> StartGame(Game game, IMessageChannel channel)
        {
            await NotifyStartGame(game);

            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Blue Team:", string.Join(' ', game.TeamBlue.Select(u => u.Mention)));
            embedBuilder.AddField("Orange Team:", string.Join(' ', game.TeamOrange.Select(u => u.Mention)));

            embedBuilder.AddField("Game Result:", $"{Output.BlueEmoji} Blue Won! {Output.OrangeEmoji} Orange Won! {Output.OvertimeEmoji} Went to OT! {Output.EndedEmoji} End Game!");

            var msg = await channel.SendMessageAsync($"**New Mafia Game! {game.Mode} with {game.Mafia.Count}**", false, embedBuilder.Build());

            var reactions = new List<IEmote>() { new Emoji(Output.BlueEmoji), new Emoji(Output.OrangeEmoji), new Emoji(Output.OvertimeEmoji), new Emoji(Output.EndedEmoji) };
            await msg.AddReactionsAsync(reactions.ToArray());

            return msg;
        }

        public static async Task<List<IUserMessage>> StartVoting(Game game, IMessageChannel channel, bool privateVoting = false)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            int i = 0;
            string players = "";
            string[] emojis = PossiblePlayerEmojis();
            foreach (var p in game.Players.Values)
            {
                p.Emoji = emojis[i++];
                players += $"{p.Emoji} - {p.Mention} ";
                if (i > 0 && i % 3 == 0) players += "\r\n";
            }

            embedBuilder.AddField("Players:", players);
            var embed = embedBuilder.Build();

            var msgs = new List<IUserMessage>();
            if(!privateVoting)
            {
                msgs.Add(await channel.SendMessageAsync($"**Vote for Mafia!**", false, embed));
            }
            else
            {   // Send each player a private DM for voting 
                foreach (var p in game.Players.Values)
                {
                    msgs.Add(await p.SendMessageAsync($"**Vote for Mafia!**", false, embed));
                }
            }

            List<IEmote> reactions = new List<IEmote>();
            foreach (var p1 in game.Players)
            {
                reactions.Add(new Emoji(p1.Value.Emoji));
            }

            foreach (var msg in msgs)
            {
                await msg.AddReactionsAsync(reactions.ToArray());
            }

            return msgs;
        }

        public static async Task<IUserMessage> Score(Game game, IMessageChannel channel)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            var ordered = game.Players.OrderByDescending(x => x.Value.Score);

            embedBuilder.AddField("Score: ", string.Join("\r\n", ordered.Select(p => $"{p.Value.Emoji} {p.Value.Mention} = {p.Value.Score}")));

            embedBuilder.AddField("Mafia: ", string.Join(" | ", game.Mafia.Select(u => $"{u.Emoji} {u.Mention}")));
            if (game.Joker != null)
                embedBuilder.AddField("Joker: ", $"{game.Joker.Emoji} {game.Joker.Mention}");

            return await channel.SendMessageAsync($"**Game Over! {ordered.First().Value.Mention} Won!**", false, embedBuilder.Build());
        }

        public static async Task<IUserMessage> HelpText(IMessageChannel channel)
        {
            var commands = typeof(MafiaCommand).GetMethods()
                      .Where(m => m.GetCustomAttributes(typeof(SummaryAttribute), false).Length > 0)
                      .ToArray();

            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (var command in commands)
            {
                var name = (CommandAttribute)command.GetCustomAttributes(typeof(CommandAttribute), false)[0];
                var summary = (SummaryAttribute)command.GetCustomAttributes(typeof(SummaryAttribute), false)[0];
                // Get the command Summary attribute information
                string embedFieldText = summary.Text ?? "No description available\n";

                embedBuilder.AddField(name.Text, embedFieldText);
            }

            return await channel.SendMessageAsync("**Mafia Commands:** ", false, embedBuilder.Build());
        }
    }
}
