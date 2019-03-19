using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TyniBot
{
    public class Emoji
    {
        [JsonProperty("emoji")]
        public string Unicode { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("shortname")]
        public string Shortname { get; set; }
    }
}
