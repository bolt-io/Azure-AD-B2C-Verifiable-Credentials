using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace did_AzFunc_api.Models;

public class AppSettingsModel
{
    public string Instance { get; set; }

    public string Endpoint { get; set; }

    public string VCServiceScope { get; set; }

    public string CredentialManifest { get; set; }

    public string IssuerAuthority { get; set; }

    public string VerifierAuthority { get; set; }

    public string TenantId { get; set; }

    public string ClientId { get; set; }

    public string ClientName { get; set; }

    public string IssuerCallbackUrlRoute { get; set; }

    public string PresentationCallbackUrlRoute { get; set; }

    public bool UseKeyVaultForSecrets { get; set; }

    public string KeyVaultName { get; set; }

    public string Authority
    {
        get
        {
            return string.Format(CultureInfo.InvariantCulture, Instance, TenantId);
        }
    }
    public string ApiEndpoint
    {
        get
        {
            return string.Format(CultureInfo.InvariantCulture, Endpoint, TenantId);
        }
    }

    public string ApiCredentialManifest
    {
        get
        {
            return string.Format(CultureInfo.InvariantCulture, CredentialManifest, TenantId);
        }
    }

    public string ClientSecret { get; set; }

    public string CertificateName { get; set; }

    public bool AppUsesClientSecret()
    {
        if (!string.IsNullOrWhiteSpace(this.ClientSecret) || this.UseKeyVaultForSecrets)
        {
            return true;
        }
        else if (!string.IsNullOrWhiteSpace(this.CertificateName))
        {
            return false;
        }
        else
        {
            throw new Exception("You must choose between using client secret or certificate. Please update the 'appsettings.json' file.");
        }
    }

    internal string PresentationCallbackUrl(HttpRequest req)
    {
        return GetRequestHostName(req) + PresentationCallbackUrlRoute;
    }

    internal string IssuerCallbackUrl(HttpRequest req)
    {
        return GetRequestHostName(req) + IssuerCallbackUrlRoute;
    }

    
    public X509Certificate2 ReadCertificate(string certificateName)
    {
        ArgumentNullException.ThrowIfNull(certificateName, nameof(certificateName));

        var certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
        var defaultCertificateLoader = new DefaultCertificateLoader();
        defaultCertificateLoader.LoadIfNeeded(certificateDescription);
        return certificateDescription.Certificate;
    }

    protected static string GetRequestHostName(HttpRequest req)
    {
        string scheme = req.Scheme;
        string originalHost = req.Headers["x-original-host"];
        string hostname;
        if (!string.IsNullOrEmpty(originalHost))
            hostname = string.Format("{0}://{1}", scheme, originalHost);
        else hostname = string.Format("{0}://{1}", scheme, req.Host);
        return hostname;
    }
}
