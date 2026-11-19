using System.Collections.Generic;
using System.Linq;
using CloudflareModule = PersonalInfrastructure.Cloudflare;
using DigitalOceanModule = PersonalInfrastructure.DigitalOcean;
using Pulumi;

return await Deployment.RunAsync(() =>
{
    var doProvider = DigitalOceanModule.ProviderFactory.Create();
    var cfProvider = CloudflareModule.ProviderFactory.Create();

    DigitalOceanModule.SshKeys.Create(doProvider);
    var (dropletUbuntu, dropletInternetCafe) = DigitalOceanModule.Droplets.Create(doProvider);
    DigitalOceanModule.Dns.Create(doProvider);
    var apps = DigitalOceanModule.Apps.Create(doProvider);

    var (zoneGiftmugweniCom, zoneFountainOfLifeFamily) = CloudflareModule.Zones.Create(cfProvider);
    CloudflareModule.Dns.Create(cfProvider);
    CloudflareModule.R2.Create(cfProvider);

    return new Dictionary<string, object?>
    {
        ["dropletUbuntuIp"] = dropletUbuntu.Ipv4Address,
        ["dropletInternetCafeIp"] = dropletInternetCafe.Ipv4Address,
        // App name → live url. Scales to any number of apps.
        ["appUrls"] = apps.ToDictionary(kv => kv.Key, kv => kv.Value.LiveUrl),
        ["zoneGiftmugweniId"] = zoneGiftmugweniCom.Id,
        ["zoneFountainOfLifeId"] = zoneFountainOfLifeFamily.Id,
    };
});
