using Pulumi;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// Registers the DigitalOcean provider. Token comes from the encrypted stack
/// config (`pulumi config set --secret digitalocean:token`).
/// </summary>
public static class ProviderFactory
{
    public static global::Pulumi.DigitalOcean.Provider Create()
    {
        var config = new Config("digitalocean");
        return new global::Pulumi.DigitalOcean.Provider("do", new global::Pulumi.DigitalOcean.ProviderArgs
        {
            Token = config.RequireSecret("token"),
        });
    }
}
