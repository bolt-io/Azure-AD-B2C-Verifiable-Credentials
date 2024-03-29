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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static did_AzFunc_api.Models.PresentationResponseModels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace did_AzFunc_api.Functions;

public class Verifier
{

    protected readonly AppSettingsModel _appSettings;
    protected IMemoryCache _cache;
    protected readonly ILogger<Verifier> _log;
    private HttpClient _httpClient;
    private IConfidentialClientApplication _msal;

    public Verifier(
        IOptions<AppSettingsModel> appSettings,
        IMemoryCache memoryCache,
        ILogger<Verifier> log,
        IHttpClientFactory httpClientFactory,
        MsalTokenProviderService msal)
    {
        _msal = msal._client;
        _appSettings = appSettings.Value;
        _cache = memoryCache;
        _log = log;
        _httpClient = httpClientFactory.CreateClient();
    }


    private PresentationRequest CreatePresentationRequest(HttpRequest req, string credType, string state)
    {
        return new PresentationRequest
        {
            IncludeQRCode = false,
            IncludeReceipt = true,
            Callback = new Callback
            {
                State = state,
                Url = _appSettings.PresentationCallbackUrl(req)
            },
            Authority = _appSettings.VerifierAuthority,
            Registration = new Registration()
            {
                ClientName = _appSettings.ClientName
            },
            RequestedCredentials = new List<RequestedCredential>()
                    {
                        new RequestedCredential
                        {
                            Type = credType,
                            Purpose = "Verifying issued credential",
                            AcceptedIssuers = new List<string>() { _appSettings.IssuerAuthority },
                            Configuration = new(){
                                Validation = new(){
                                    AllowRevoked = false,
                                    ValidateLinkedDomain = true
                            }
                        }
                    }
            }

        };
    }

    [FunctionName("presentation-request")]
    public async Task<ActionResult> PresentationRequest([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/api/verifier/presentation-request")] HttpRequest req)
    {
        try
        {
            string credType = req.Query["credType"];
            string stateProperties = req.Query["StateProperties"];


            _log.LogInformation($"Starting new verification request for credential type {credType}");
            var t = await _msal.AcquireTokenForClient(new[] { _appSettings.VCServiceScope }).ExecuteAsync();
            if (t.AccessToken == String.Empty)
            {
                _log.LogError("Failed to acquire accesstoken");
                return new BadRequestObjectResult(new { error = "no access token", error_description = "Authentication Failed" });
            }
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t.AccessToken);

            string state = stateProperties ?? Guid.NewGuid().ToString();
            

            var jsonString = JsonSerializer.Serialize(CreatePresentationRequest(req, credType, state));
            try
            {
                var presentationRequestEndpoint = _appSettings.ApiEndpoint + "verifiableCredentials/createPresentationRequest";
                _log.LogInformation($"presentation-request: Sending {jsonString} to VC service {presentationRequestEndpoint}");
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var serviceRequest = await _httpClient.PostAsync(presentationRequestEndpoint, content);
                var response = await serviceRequest.Content.ReadAsStringAsync();
                _httpClient.Dispose();
                var res = System.Text.Json.JsonSerializer.Deserialize<VcResponse>(response);

                _log.LogTrace("succesfully called Request API");
                var statusCode = serviceRequest.StatusCode;

                if (statusCode == HttpStatusCode.Created)
                {
                    var cacheData = new CacheObject
                    {
                        Id = state,
                        Url = res.Url,
                        Status = "notscanned",
                        Message = "Request ready, please scan with Authenticator",
                        Expiry = res.Expiry.ToString()
                    };
                    _cache.Set(state, JsonSerializer.Serialize(cacheData));

                    return new OkObjectResult(cacheData);
                }
                else
                {
                    _log.LogError("Unsuccesfully called Request API: {response}", response);
                    return new BadRequestObjectResult(new { error = "400", error_description = "Something went wrong calling the API: " + response });
                }
            }
            catch (Exception ex)
            {
                _log.LogError("Unsuccesfully called Request API", ex);

                return new BadRequestObjectResult(new { error = "400", error_description = "Something went wrong calling the API: " + ex.Message });
            }
        }
        catch (Exception ex)
        {
            _log.LogError("Unsuccesfully called Request API", ex);
            return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
        }
    }

    [FunctionName("presentation-callback")]
    public async Task<ActionResult> PresentationCallback([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/verifier/presentation-callback")] HttpRequest req)
    {
        try
        {
            string content = await new StreamReader(req.Body).ReadToEndAsync();

            var presentation = string.IsNullOrEmpty(content)
                ? null
                : JsonSerializer.Deserialize<PresentationCallback>(content);

            if (presentation == null)
            {
                _log.LogError("No presentation response received in body");
            }

            Debug.WriteLine("callback!: " + presentation.RequestId);
            var requestId = presentation.RequestId;
            var state = presentation.State;

            if (presentation.RequestStatus.Equals("request_retrieved", StringComparison.InvariantCultureIgnoreCase))
            {
                var cacheData = new CacheObject
                {
                    Status = "request_retrieved",
                    Message = "QR Code is scanned. Waiting for validation...",
                };
                _cache.Set(state, JsonSerializer.Serialize(cacheData));
                _log.LogInformation("QR Code is scanned. Waiting for validation...");
            }

            if (presentation.RequestStatus.Equals("presentation_verified", StringComparison.CurrentCultureIgnoreCase))
            {
                _log.LogInformation("Presentation verified");
                var cacheData = new CacheObject
                {
                    Status = "presentation_verified",
                    Message = "Presentation verified",
                    Payload = JsonSerializer.Serialize(presentation.VerifiedCredentialsData),
                    Subject = presentation.Subject,
                    FullName = $"{presentation.VerifiedCredentialsData.First().Claims.FirstName} {presentation.VerifiedCredentialsData.First().Claims.LastName}",
                    FirstName = presentation.VerifiedCredentialsData.First().Claims.FirstName,
                    LastName = presentation.VerifiedCredentialsData.First().Claims.LastName,
                    TenantObjectId = presentation.VerifiedCredentialsData.First().Claims.TenantObjectId,
                };
                _cache.Set(state, JsonSerializer.Serialize(cacheData));
                _log.LogInformation("presentation verified and cached");
            }

            return new OkResult();
        }
        catch (Exception ex)
        {
            _log.LogError("Unsuccesfully called Request API", ex);
            return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
        }
    }

    [FunctionName("presentation-response")]
    public ActionResult PresentationResponse([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/api/verifier/presentation-response")] HttpRequest req)
    {
        try
        {


            string state = req.Query["id"];
            //var state = id;
            if (string.IsNullOrEmpty(state))
            {
                return new BadRequestObjectResult(new { error = "400", error_description = "Missing argument 'id'" });
            }
            if (_cache.TryGetValue(state, out string buf))
            {
                var cacheObject = JsonSerializer.Deserialize<CacheObject>(buf);

                Debug.WriteLine("check if there was a response yet: " + cacheObject.Status);
                return new ContentResult { ContentType = "application/json", Content = JsonSerializer.Serialize(cacheObject) };
            }

            return new OkResult();
        }
        catch (Exception ex)
        {
            _log.LogError("Unsuccesfully called reponse API", ex);
            return new BadRequestObjectResult(new { error = "400", error_description = ex.Message });
        }
    }
}
