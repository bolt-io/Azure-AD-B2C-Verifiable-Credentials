
using did_AzFunc_api.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace did_AzFunc_api.Services;


public static class DidWellKnownEndpointService
{
    private static DidConfiguration _configuration = null;

    public static async Task<DidConfiguration> GetDidConfiguration()
    {

        if (_configuration == null)
        {
            var json = await File.ReadAllTextAsync("did-configuration.json");
            _configuration = JsonSerializer.Deserialize<DidConfiguration>(json);
        }

        return _configuration;
    }
}
