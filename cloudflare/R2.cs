using Pulumi;
using CF = Pulumi.Cloudflare;

namespace PersonalInfrastructure.Cloudflare;

/// <summary>
/// The two R2 buckets and the custom domain binding bucket.giftmugweni.com.
/// The custom domain binding is state-injected (see ledger): the provider
/// does not implement pulumi import for R2CustomDomain — the state was
/// seeded by hand from the live API response and verified by preview.
/// </summary>
public static class R2
{
    public static void Create(global::Pulumi.Cloudflare.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        new CF.R2Bucket("apps-bucket", new CF.R2BucketArgs
        {
            AccountId = Zones.AccountId,
            Name = "apps-bucket",
            Location = "WEUR",
            StorageClass = "Standard",
            Jurisdiction = "default",
        }, opts);

        new CF.R2Bucket("public-apps-bucket", new CF.R2BucketArgs
        {
            AccountId = Zones.AccountId,
            Name = "public-apps-bucket",
            Location = "WEUR",
            StorageClass = "Standard",
            Jurisdiction = "default",
        }, opts);

        new CF.R2CustomDomain("public-apps-bucket-custom-domain", new CF.R2CustomDomainArgs
        {
            AccountId = Zones.AccountId,
            BucketName = "public-apps-bucket",
            Domain = "bucket.giftmugweni.com",
            ZoneId = "b5ddc47f934ad6a572fba57177c9987f",
            Enabled = true,
            MinTls = "1.0",
            Jurisdiction = "default",
        }, opts);
    }
}
