using Pulumi;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// The two Droplets. The default lon1 VPC
/// (11799a23-7c4c-4f53-9c4b-b027138bf246) is deliberately not imported as a
/// resource — it is auto-created DO infrastructure, referenced here as a
/// constant.
/// </summary>
public static class Droplets
{
    // Default VPC in lon1 (auto-created by DO; not manageable/imported).
    public const string DefaultVpcLon1 = "11799a23-7c4c-4f53-9c4b-b027138bf246";

    public static (DO.Droplet UbuntuLon1, DO.Droplet InternetCafeServer) Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        var ubuntuLon1 = new DO.Droplet("ubuntu-s-1vcpu-1gb-lon1-01", new DO.DropletArgs
        {
            Name = "ubuntu-s-1vcpu-1gb-lon1-01",
            // Note: resource name is misleading — this droplet is actually
            // s-1vcpu-2gb, built from snapshot 194230265. Cleanup phase will
            // rename (rename = recreate, so it stays as-is for now).
            Image = "194230265",
            Size = "s-1vcpu-2gb",
            Region = "lon1",
            VpcUuid = DefaultVpcLon1,
            Tags = new[] { "server", "projects" },
            Backups = true,
            Monitoring = true,
            Ipv6 = true,
            // DropletAgent/GracefulShutdown deliberately unset: import stored
            // them as null and they are ForceNew — setting them would replace
            // the live droplet.
        }, opts);

        var internetCafeServer = new DO.Droplet("internet-cafe-server", new DO.DropletArgs
        {
            Name = "internet-cafe-server",
            // Numeric image id (ubuntu 24.04 LTS x64 base image, id 195932981):
            // the slug forces replacement on import-matched state.
            Image = "195932981",
            Size = "s-1vcpu-1gb",
            Region = "lon1",
            VpcUuid = DefaultVpcLon1,
            Tags = new[] { "radiusdesk", "wireguard" },
            Ipv6 = true,
        }, opts);

        return (ubuntuLon1, internetCafeServer);
    }
}
