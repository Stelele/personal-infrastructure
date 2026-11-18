using Pulumi;

namespace PersonalInfrastructure.Cloudflare;

/// <summary>
/// Registers the Cloudflare provider. Token comes from the encrypted stack
/// config (`pulumi config set --secret cloudflare:apiToken`).
/// </summary>
public static class ProviderFactory
{
    public static global::Pulumi.Cloudflare.Provider Create()
    {
        var config = new Config("cloudflare");
        return new global::Pulumi.Cloudflare.Provider("cf", new global::Pulumi.Cloudflare.ProviderArgs
        {
            ApiToken = config.RequireSecret("apiToken"),
        });
    }
}
