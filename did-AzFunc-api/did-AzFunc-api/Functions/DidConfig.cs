using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using did_AzFunc_api.Models;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace did_AzFunc_api;

public class DidConfig
{
    private readonly DidConfiguration _configuration = null;
    
    private readonly ILogger<DidConfig> _log;

    public DidConfig(IOptions<DidConfiguration> appSettings, ILogger<DidConfig> log)
    {
        _configuration= appSettings.Value;
        _log = log;
    }

    [FunctionName("did-config")]
    public IActionResult GetDidConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = ".well-known/did-configuration.json")] HttpRequest req)
    {
        _log.LogTrace($"did configuration requested");

        var result = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        return new OkObjectResult(result);
    }
}
