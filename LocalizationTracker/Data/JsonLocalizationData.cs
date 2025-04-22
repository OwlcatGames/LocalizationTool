using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalizationTracker.Data
{
    public class JsonLocalizationData
    {
        [JsonInclude]
        [JsonPropertyName("$id")]
        public string Id { get; set; }

        [JsonInclude]
        [JsonPropertyName("strings")]
        public Dictionary<string, string> Strings { get; set; }
    }
}
