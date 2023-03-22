using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace did_AzFunc_api.Models;

public class VcResponse
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("expiry")]
    public long Expiry { get; set; }

    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; }

    public string Pin { get; set; }
}

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class Headers
{
    [JsonPropertyName("api-key")]
    public string ApiKey { get; set; }
}

public class Callback
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("headers")]
    public Headers Headers { get; set; }
}

public class Registration
{
    [JsonPropertyName("clientName")]
    public string ClientName { get; set; }
}

public class Pin
{
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }
}

public class Claims
{
    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }
}

public class Issuance
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("manifest")]
    public string Manifest { get; set; }

    [JsonPropertyName("pin")]
    public Pin Pin { get; set; }

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; }
}

public class IssuanceRequest
{
    public string Id { get; set; }


    [JsonPropertyName("includeQRCode")]
    public bool IncludeQRCode { get; set; }

    [JsonPropertyName("callback")]
    public Callback Callback { get; set; }

    [JsonPropertyName("authority")]
    public string Authority { get; set; }

    [JsonPropertyName("registration")]
    public Registration Registration { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("manifest")]
    public string Manifest { get; set; }

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; }

    [JsonPropertyName("pin")]
    public Pin Pin { get; set; }
}

public class PresentationRequest
{
    [JsonPropertyName("includeQRCode")]
    public bool IncludeQRCode { get; set; }
    
    [JsonPropertyName("includeReceipt")]
    public bool IncludeReceipt { get; set; }

    [JsonPropertyName("callback")]
    public Callback Callback { get; set; }

    [JsonPropertyName("authority")]
    public string Authority { get; set; }

    [JsonPropertyName("registration")]
    public Registration Registration { get; set; }


    [JsonPropertyName("requestedCredentials")]
    public List<RequestedCredential> RequestedCredentials { get; set; }
}

public class RequestedCredential
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; }

    [JsonPropertyName("acceptedIssuers")]
    public List<string> AcceptedIssuers { get; set; }

    [JsonPropertyName("configuration")]
    public RequestedCredentialConfiguration Configuration { get; set; } = new();
}

public class Presentation
{
    [JsonPropertyName("includeReceipt")]
    public bool IncludeReceipt { get; set; }

    [JsonPropertyName("requestedCredentials")]
    public List<RequestedCredential> RequestedCredentials { get; set; }
}

public class RequestedCredentialConfiguration
{
    public RequestedCredentialValidation Validation { get; set; } = new();
}

public class RequestedCredentialValidation
{
    [JsonPropertyName("allowRevoked")]
    public bool AllowRevoked { get; set; } = false;
    [JsonPropertyName("validateLinkedDomain")]
    public bool ValidateLinkedDomain { get; set; } = true;
}

public class IssuanceCallback
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("error")]
    public Error Error { get; set; }
}

public class Error
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}

public class CacheObject
{
    
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; }

    [JsonPropertyName("expiry")]
    public string Expiry { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; }
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
    [JsonPropertyName("tenantObjectId")]
    public object TenantObjectId { get; set; }
}
