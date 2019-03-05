using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TyniBot
{
    public class BotSettings
    {
        [JsonProperty(Required = Required.Always)]
        public string BotToken { get; set; }
    }
}
