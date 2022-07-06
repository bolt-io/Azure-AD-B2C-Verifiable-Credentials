using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace did_AzFunc_api.Models;

public class DidConfiguration
{
    [JsonPropertyName("@context")]
    public string Context { get; set; }

    [JsonPropertyName("linked_dids")]
    public List<string> LinkedDids { get; set; }
}
