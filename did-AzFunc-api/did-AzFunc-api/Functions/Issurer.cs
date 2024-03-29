using did_AzFunc_api.Models;
using did_AzFunc_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace did_AzFunc_api.Functions
{
    public class Issurer
    {
        protected readonly AppSettingsModel _appSettings;
        protected IMemoryCache _cache;
        protected readonly ILogger<Issurer> _log;
        private HttpClient _httpClient;
        private readonly IConfidentialClientApplication _msal;


        public Issurer(IOptions<AppSettingsModel> appSettings, MsalTokenProviderService msal, IMemoryCache memoryCache, ILogger<Issurer> log, IHttpClientFactory httpClientFactory)
        {
            _appSettings = appSettings.Value;
            _cache = memoryCache;
            _log = log;
            _httpClient = httpClientFactory.CreateClient();
            _msal = msal._client;
        }

        private string GetRandomPIN(int length)
        {
            var pinMaxValue = (int)Math.Pow(10, length) - 1;
            var randomNumber = RandomNumberGenerator.GetInt32(1, pinMaxValue);
            return string.Format("{0:D" + length.ToString() + "}", randomNumber);
        }
        private IssuanceRequest GenerateVcRequest(HttpRequest req, string reqId, string credType, string firstName, string lastName)
        {

            var credentialGuid = _appSettings.CredentialMaps[credType];
            _log.LogInformation($"Credential GUID: {credentialGuid}");

            var request = new IssuanceRequest()
            {
                Id = reqId,
                IncludeQRCode = true,
                Callback = new Callback()
                {
                    Url = _appSettings.IssuerCallbackUrl(req),
                    State = reqId,
                },
                Authority = _appSettings.IssuerAuthority,
                Registration = new Registration()
                {
                    ClientName = _appSettings.ClientName
                },

                Type = credType,
                Manifest = $"{_appSettings.ApiCredentialManifest}{credentialGuid}/manifest",
                Pin = IsMobile(req) ? null : new Pin()
                {
                    Value = GetRandomPIN(4),
                    Length = 4
                },
                Claims = new Claims()
                {
                    FamilyName = lastName,
                    GivenName = firstName
                }

            };
            return request;
        }

        [FunctionName("Issuance-request")]
        public async Task<ActionResult> IssuanceRequest([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/api/issuer/issuance-request")] HttpRequest req)
        {
            try
            {
                string credType = req.Query["credType"];
                string firstName = req.Query["firstName"];
                string lastName = req.Query["lastName"];

                string jsonResponse = string.Empty;
                var requestId = Guid.NewGuid().ToString();

                _log.LogInformation($"Starting new issue request for credtype: {credType}");

                var authenticationResult = await _msal.AcquireTokenForClient(new[] { _appSettings.VCServiceScope }).ExecuteAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

                _log.LogInformation($"Generating request ID {requestId}");
                var request = GenerateVcRequest(req, requestId, credType, firstName, lastName);

                var reqPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                //CALL REST API WITH PAYLOAD
                HttpStatusCode statusCode = HttpStatusCode.OK;

                try
                {
                    _log.LogInformation($"Sending {reqPayload} to VC service {_appSettings.Endpoint}");

                    //var serviceRequest = await _httpClient.PostAsJsonAsync<VcRequest>("verifiablecredentials/request", request);
                    var a = new StringContent(reqPayload, System.Text.Encoding.UTF8, "application/json");

                    var serviceRequest = await _httpClient.PostAsync(_appSettings.ApiEndpoint + "verifiableCredentials/createIssuanceRequest", a);

                    //var response = await serviceRequest.Content.ReadAsStringAsync();
                    var response = await serviceRequest.Content.ReadAsStringAsync();
                    _httpClient.Dispose();
                    var res = System.Text.Json.JsonSerializer.Deserialize<VcResponse>(response);

                    statusCode = serviceRequest.StatusCode;

                    if (statusCode == HttpStatusCode.Created)
                    {
                        _log.LogTrace("succesfully called Request API");

                        //We use in memory cache to keep state about the request. The UI will check the state when calling the presentationResponse method

                        var cacheData = new CacheObject
                        {
                            Status = "notscanned",
                            Message = "Request ready, please scan with Authenticator",
                            Expiry = res.Expiry.ToString()
                        };
                        _cache.Set(requestId, JsonSerializer.Serialize(cacheData));

                        res.Pin = IsMobile(req) ? null : request.Pin.Value;
                        return new OkObjectResult(res);
                    }
                    else
                    {
                        _log.LogError("Unsuccesfully called Request API");
                        return new BadRequestObjectResult(new { error = "400", error_description = "Something went wrong calling the API: " + response });
                    }

                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult(new { error = "400", error_description = "Something went wrong calling the API: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
            }
        }

        [FunctionName("Issuance-callback")]
        public async Task<ActionResult> IssuanceCallback([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "/api/issuer/issuancecallback")] HttpRequest req)
        {
            try
            {
                string content = await new StreamReader(req.Body).ReadToEndAsync();

                var issuanceResponse = string.IsNullOrEmpty(content)
                    ? null
                    : JsonSerializer.Deserialize<IssuanceCallback>(content);

                if (issuanceResponse == null)
                {
                    _log.LogError("No issuance response received in body");
                }

                _log.LogTrace($"callback!: {issuanceResponse.RequestId}");
                var requestId = issuanceResponse.RequestId;

                if (issuanceResponse.Code.Equals("request_retrieved", StringComparison.InvariantCultureIgnoreCase))
                {
                    var cacheData = new CacheObject
                    {
                        Status = "request_retrieved",
                        Message = "QR Code is scanned. Waiting for issuance...",
                    };
                    _cache.Set(requestId, JsonSerializer.Serialize(cacheData));
                }

                if (issuanceResponse.Code.Equals("issuance_successful", StringComparison.InvariantCultureIgnoreCase))
                {
                    var cacheData = new CacheObject
                    {
                        Status = "issuance_successful",
                        Message = "Credential issued successfully",
                    };

                    _cache.Set(requestId, JsonSerializer.Serialize(cacheData));
                }
                //
                //We capture if something goes wrong during issuance. See documentation with the different error codes
                //
                if (issuanceResponse.Code.Equals("issuance_error", StringComparison.InvariantCultureIgnoreCase))
                {
                    var cacheData = new CacheObject
                    {
                        Status = "issuance_error",
                        Payload = issuanceResponse.Error.Code.ToString(),
                        Message = issuanceResponse.Error.Message

                    };
                    _cache.Set(requestId, JsonSerializer.Serialize(cacheData));
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
            }
        }

        [FunctionName("issuance-response")]
        public ActionResult IssuanceResponse([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/api/issuer/issuance-response")] HttpRequest req)
        {
            try
            {
                string requestId = req.Query["id"];
                if (string.IsNullOrEmpty(requestId))
                {
                    return new BadRequestObjectResult(new { error = "400", error_description = "Missing argument 'id'" });
                }
                if (_cache.TryGetValue(requestId, out string buf))
                {
                    var cacheObject = JsonSerializer.Deserialize<CacheObject>(buf);

                    Debug.WriteLine("check if there was a response yet: " + cacheObject.Status);
                    return new ContentResult { ContentType = "application/json", Content = JsonSerializer.Serialize(cacheObject) };
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
            }
        }

        protected static bool IsMobile(HttpRequest req)
        {
            string userAgent = req.Headers["User-Agent"];

            if (userAgent.Contains("Android") || userAgent.Contains("iPhone"))
                return true;
            else
                return false;
        }

    }

}