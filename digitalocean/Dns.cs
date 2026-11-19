using Pulumi;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// LEGACY/STALE ZONE: the domain is delegated to Cloudflare nameservers; this
/// DigitalOcean zone is a leftover duplicate and is scheduled for deletion in
/// the cleanup phase. Imported here only so the estate is fully accounted
/// for (owner decision 2026-11-18, see docs/superpowers/specs).
/// </summary>
public static class Dns
{
    private static readonly (string Name, string RecordName, string Type, string Data, int Ttl)[] Records =
    {
        ("apex-soa", "@", "SOA", "1800", 1800),
        ("apex-ns-1", "@", "NS", "ns1.digitalocean.com.", 1800),
        ("apex-ns-2", "@", "NS", "ns2.digitalocean.com.", 1800),
        ("apex-ns-3", "@", "NS", "ns3.digitalocean.com.", 1800),
        ("apex-a-cf-1", "@", "A", "162.159.140.98", 30),
        ("apex-aaaa-cf-1", "@", "AAAA", "2606:4700:7::60", 30),
        ("apex-aaaa-cf-2", "@", "AAAA", "2a06:98c1:58::60", 30),
        ("apex-a-cf-2", "@", "A", "172.66.0.96", 30),
        ("api-a", "api", "A", "167.99.93.239", 3600),
    };

    public static void Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        new DO.Domain("giftmugweni-com", new DO.DomainArgs
        {
            Name = "giftmugweni.com",
        }, opts);

        foreach (var (label, recordName, type, data, ttl) in Records)
        {
            new DO.DnsRecord($"do-record-{label}", new DO.DnsRecordArgs
            {
                Domain = "giftmugweni.com",
                Name = recordName,
                Type = type,
                Value = data,
                Ttl = ttl,
            }, opts);
        }
    }
}
