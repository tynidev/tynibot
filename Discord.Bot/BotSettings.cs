using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Bot
{
    public class BotSettings
    {
        [JsonProperty(Required = Required.Always)]
        public string BotToken { get; set; }

        
        [JsonProperty(Required = Required.Always)]
        public string ApplicationId { get; set; }
    }
}
