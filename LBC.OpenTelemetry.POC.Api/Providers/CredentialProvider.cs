using Azure.Identity;

namespace LBC.OpenTelemetry.POC.Api.Providers;

public static class CredentialProvider
{
    public static ChainedTokenCredential GetCredentials()
       => new(
           new AzureCliCredential(),
           new ManagedIdentityCredential(),
           new AzurePowerShellCredential());
}