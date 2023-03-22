using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace did_AzFunc_api.Models
{
    public class PresentationResponseModels
    {
        public class Claims
        {
            [JsonPropertyName("firstName")]
            public string FirstName { get; set; }
            [JsonPropertyName("lastName")]
            public string LastName { get; set; }
            [JsonPropertyName("tenantObjectId")]
            public object TenantObjectId { get; set; }
        }

        public class VerifiedCredentialsData
        {
            [JsonPropertyName("issuer")]
            public string VCIssuer { get; set; }

            [JsonPropertyName("type")]
            public List<string> Type { get; set; }

            [JsonPropertyName("claims")]
            public Claims Claims { get; set; }

            [JsonPropertyName("credentialState")]
            public CredentialState CredentialState { get; set; }

            [JsonPropertyName("domainValidation")]
            public DomainValidation DomainValidation { get; set; }

        }

        public class DomainValidation
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class CredentialState
        {
            [JsonPropertyName("revocationStatus")]
            public string RevocationStatus { get; set; }
        }

        public class Receipt
        {
            [JsonPropertyName("id_token")]
            public string IdToken { get; set; }
            [JsonPropertyName("vp_token")]
            public string VpToken { get; set; }
            [JsonPropertyName("state")]
            public string State { get; set; }
        }

        public class PresentationCallback
        {
            // https://learn.microsoft.com/en-us/azure/active-directory/verifiable-credentials/presentation-request-api#callback-events

            [JsonPropertyName("requestId")]
            public string RequestId { get; set; }

            [JsonPropertyName("requestStatus")]
            public string RequestStatus { get; set; }

            [JsonPropertyName("state")]
            public string State { get; set; }

            [JsonPropertyName("subject")]
            public string Subject { get; set; }

            [JsonPropertyName("verifiedCredentialsData")]
            public List<VerifiedCredentialsData> VerifiedCredentialsData { get; set; } = new List<VerifiedCredentialsData>();

            [JsonPropertyName("receipt")]
            public Receipt Receipt { get; set; } = null;
        }
    }
}
