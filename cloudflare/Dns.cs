using Pulumi;
using CF = Pulumi.Cloudflare;

namespace PersonalInfrastructure.Cloudflare;

/// <summary>
/// The 24 manually-managed Cloudflare DNS records across the two zones
/// (2026-11-18 survey). TXT content includes the surrounding double quotes
/// the CF API returns — copied byte-for-byte.
///
/// Deliberately NOT managed here (owner decision 2026-11-19):
///  • bucket.giftmugweni.com CNAME — auto-created and managed by Cloudflare
///    when the R2 custom domain (R2.cs) was attached;
///  • 3 apex NS records pointing at DO nameservers and 4 apex A/AAAA records
///    whose content is Cloudflare's own anycast IPs — migration artifacts,
///    left live but unmanaged; deletion in the phase-3 cleanup.
///  (Note: portal/erpnext records are dead and scheduled for cleanup too,
///   but they are manual records, so they stay managed until then.)
/// </summary>
public static class Dns
{
    private static readonly (string Zone, string Label, string Name, string Type, string Content, bool Proxied, int Ttl, int? Priority, string Comment)[] Records =
    {
        ("giftmugweni-com", "giftmugweni-com-api-a", "api.giftmugweni.com", "A", "167.99.93.239", true, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com-api-cms-a", "api-cms.giftmugweni.com", "A", "167.99.93.239", true, 1, null, "API backend for cms system"),
        ("giftmugweni-com", "giftmugweni-com-api-cms-aaaa", "api-cms.giftmugweni.com", "AAAA", "2a03:b0c0:1:e0:0:1:2719:3001", true, 1, null, "API backend for cms system"),
        ("giftmugweni-com", "giftmugweni-com-api-erp-dashboard-a", "api-erp-dashboard.giftmugweni.com", "A", "167.99.93.239", true, 1, null, "Api endpoint for Frappe Erpnext backend server api for custom dashboard"),
        ("giftmugweni-com", "giftmugweni-com-api-erp-dashboard-aaaa", "api-erp-dashboard.giftmugweni.com", "AAAA", "2a03:b0c0:1:e0:0:1:2719:3001", true, 1, null, "Api endpoint for Frappe Erpnext backend server api for custom dashboard"),
        ("giftmugweni-com", "giftmugweni-com-api-erp-dashboard-a-2", "api.erp-dashboard.giftmugweni.com", "A", "167.99.93.239", false, 1, null, "Api endpoint for Frappe Erpnext backend server api for custom dashboard"),
        ("giftmugweni-com", "giftmugweni-com-api-erp-dashboard-aaaa-2", "api.erp-dashboard.giftmugweni.com", "AAAA", "2a03:b0c0:1:e0:0:1:2719:3001", false, 1, null, "Api endpoint for Frappe Erpnext backend server api for custom dashboard"),
        ("giftmugweni-com", "giftmugweni-com-status-radius-a", "status-radius.giftmugweni.com", "A", "165.232.33.196", true, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com-erpnext-a", "erpnext.giftmugweni.com", "A", "139.59.7.15", true, 1, null, "erpnext integration"),
        ("giftmugweni-com", "giftmugweni-com-openpos-a", "openpos.giftmugweni.com", "A", "167.99.93.239", true, 1, null, "point of sale testing"),
        ("giftmugweni-com", "giftmugweni-com-lessons-a", "lessons.giftmugweni.com", "A", "165.232.33.196", true, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com-lessons-aaaa", "lessons.giftmugweni.com", "AAAA", "2a03:b0c0:1:e0:0:1:6ea7:7001", true, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com-portal-a", "portal.giftmugweni.com", "A", "161.35.0.160", false, 1, null, "Radius Captive Portal Entry"),
        ("giftmugweni-com", "giftmugweni-com-portal-aaaa", "portal.giftmugweni.com", "AAAA", "2604:a880:400:d1:0:4:557f:6001", false, 1, null, "Radius Captive Portal"),
        ("giftmugweni-com", "giftmugweni-com-radius-a", "radius.giftmugweni.com", "A", "165.232.33.196", true, 1, null, "Radius desk ui"),
        ("giftmugweni-com", "giftmugweni-com-radius-aaaa", "radius.giftmugweni.com", "AAAA", "2a03:b0c0:1:e0:0:1:6ea7:7001", true, 1, null, "Radius Desk Webui"),
        ("giftmugweni-com", "giftmugweni-com-reader-cname", "reader.giftmugweni.com", "CNAME", "plankton-app-5zhzx.ondigitalocean.app", true, 1, null, "AI Voice generation website"),
        ("giftmugweni-com", "giftmugweni-com-send-mx", "send.giftmugweni.com", "MX", "feedback-smtp.eu-west-1.amazonses.com", false, 1, 10, ""),
        ("giftmugweni-com", "giftmugweni-com-dmarc-txt", "_dmarc.giftmugweni.com", "TXT", "\"v=DMARC1; p=none;\"", false, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com--txt", "giftmugweni.com", "TXT", "\"google-site-verification=d3bvCk0pp28CptdksIwrm-QogVKzfxi3mgZh9weT7IQ\"", false, 3600, null, ""),
        ("giftmugweni-com", "giftmugweni-com-resend-domainkey-txt", "resend._domainkey.giftmugweni.com", "TXT", "\"p=MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCliD/CVdgDc7NX99U2ezORl+enhXtCiNI6fTTWfbim1DH5kPPSB9neF93tjs+9ZGFziWwv8QqLpGRYv9H5HwgkpFLwrfTI6sGV9rTfMWl1ygdyraow0Nfx0sqD3lHv6L5yyOPxheQDNEmdzYZeZlEUdKmGU8ote4oNd5q73ycFqwIDAQAB\"", false, 1, null, ""),
        ("giftmugweni-com", "giftmugweni-com-send-txt", "send.giftmugweni.com", "TXT", "\"v=spf1 include:amazonses.com ~all\"", false, 1, null, ""),
        ("fountainoflifefamily-com", "fountainoflifefamily-com--cname", "fountainoflifefamily.com", "CNAME", "seahorse-app-ll9cl.ondigitalocean.app", true, 1, null, ""),
        ("fountainoflifefamily-com", "fountainoflifefamily-com-www-cname", "www.fountainoflifefamily.com", "CNAME", "seahorse-app-ll9cl.ondigitalocean.app", true, 1, null, ""),
    };

    public static void Create(global::Pulumi.Cloudflare.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        foreach (var (zone, label, name, type, content, proxied, ttl, priority, comment) in Records)
        {
            var args = new CF.DnsRecordArgs
            {
                ZoneId = zone == "giftmugweni-com" ? "b5ddc47f934ad6a572fba57177c9987f" : "a13f8c191a8aca0bef3cc2652713d427",
                Name = name,
                Type = type,
                Content = content,
                Proxied = proxied,
                Ttl = ttl,
            };
            if (priority is not null) args.Priority = priority.Value;
            if (comment.Length > 0) args.Comment = comment;
            new CF.DnsRecord($"cf-record-{label}", args, opts);
        }
    }
}
