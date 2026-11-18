using System.Collections.Generic;
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
    var (appPersonalSite, appVoiceGeneration, appFountainOfLife, appNjeremotoDashboards, appNbiApp) = DigitalOceanModule.Apps.Create(doProvider);

    var (zoneGiftmugweniCom, zoneFountainOfLifeFamily) = CloudflareModule.Zones.Create(cfProvider);
    CloudflareModule.Dns.Create(cfProvider);
    CloudflareModule.R2.Create(cfProvider);

    return new Dictionary<string, object?>
    {
        ["dropletUbuntuIp"] = dropletUbuntu.Ipv4Address,
        ["dropletInternetCafeIp"] = dropletInternetCafe.Ipv4Address,
        ["appPersonalSiteUrl"] = appPersonalSite.LiveUrl,
        ["appVoiceGenerationUrl"] = appVoiceGeneration.LiveUrl,
        ["appFountainOfLifeUrl"] = appFountainOfLife.LiveUrl,
        ["appNjeremotoDashboardUrl"] = appNjeremotoDashboards.LiveUrl,
        ["appNbiAppUrl"] = appNbiApp.LiveUrl,
        ["zoneGiftmugweniId"] = zoneGiftmugweniCom.Id,
        ["zoneFountainOfLifeId"] = zoneFountainOfLifeFamily.Id,
    };
});
