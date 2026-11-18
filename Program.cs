using System.Collections.Generic;
using CloudflareModule = PersonalInfrastructure.Cloudflare;
using DigitalOceanModule = PersonalInfrastructure.DigitalOcean;
using Pulumi;

return await Deployment.RunAsync(() =>
{
    var doProvider = DigitalOceanModule.ProviderFactory.Create();
    var cfProvider = CloudflareModule.ProviderFactory.Create();

    DigitalOceanModule.SshKeys.Create(doProvider);
    _ = DigitalOceanModule.Droplets.Create(doProvider);
    DigitalOceanModule.Dns.Create(doProvider);

    // Resource modules (imported in phase 1) plug in here:
    // DigitalOcean: SshKeys, Droplets, Apps, Dns
    // Cloudflare:   Zones, Dns, R2

    return new Dictionary<string, object?>
    {
        // Outputs (app urls, droplet ips, zone ids) land here once the
        // modules exist.
    };
});
