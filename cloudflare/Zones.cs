using Pulumi;
using CF = Pulumi.Cloudflare;

namespace PersonalInfrastructure.Cloudflare;

/// <summary>
/// The two Cloudflare zones. Account id is an identifier (not a secret) and
/// is inlined per the design doc.
/// </summary>
public static class Zones
{
    public const string AccountId = "07e0cf4e7092208301d9db7ca03e3a77";

    public static (CF.Zone GiftMugweniCom, CF.Zone FountainOfLifeFamily) Create(global::Pulumi.Cloudflare.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        var giftMugweniCom = new CF.Zone("giftmugweni-com", new CF.ZoneArgs
        {
            Name = "giftmugweni.com",
            Account = new CF.Inputs.ZoneAccountArgs { Id = AccountId },
            Type = "full",
        }, opts);

        var fountainOfLifeFamily = new CF.Zone("fountainoflifefamily-com", new CF.ZoneArgs
        {
            Name = "fountainoflifefamily.com",
            Account = new CF.Inputs.ZoneAccountArgs { Id = AccountId },
            Type = "full",
        }, opts);

        return (giftMugweniCom, fountainOfLifeFamily);
    }
}
