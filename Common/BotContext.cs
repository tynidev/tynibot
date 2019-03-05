using Discord;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace TyniBot
{
    public class BotContext
    {
        public DiscordSocketClient Client { get; }
        public LiteDatabase Database { get; }
        public BotSettings Settings { get; }

        public BotContext(DiscordSocketClient client, LiteDatabase database, BotSettings settings)
        {
            Client = client;
            Database = database;
            Settings = settings;
        }
    }
}
