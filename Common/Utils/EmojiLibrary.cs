using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TyniBot
{
    public sealed class EmojiLibrary
    {
        private static EmojiLibrary instance = null;
        private static readonly object padlock = new object();

        private Emoji[] emojis;
        private EmojiLibrary()
        {
            // read emojis.json
            emojis = JsonConvert.DeserializeObject<Emoji[]>(File.ReadAllText("Utils\\emojis.json"));
        }

        public static EmojiLibrary Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new EmojiLibrary();
                    }
                    return instance;
                }
            }
        }

        public static Emoji ByShortname(string name)
        {
            return Instance.emojis.Where(e => e.Shortname == name).FirstOrDefault();
        }
    }
}
