using Pulumi;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// SSH keys registered with DigitalOcean. Imported in phase 1; keys map 1:1 to
/// the live account (ids 55949597, 49471038, 46280680).
/// </summary>
public static class SshKeys
{
    public static void Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        new DO.SshKey("personal-nobara-machine", new DO.SshKeyArgs
        {
            Name = "Personal Nobara Machine",
            PublicKey = "[managed via Pulumi secret config sshKeyPersonalNobara]",
        }, opts);

        new DO.SshKey("new-personal-laptop", new DO.SshKeyArgs
        {
            Name = "New Personal Laptop",
            PublicKey = "[managed via Pulumi secret config sshKeyNewPersonalLaptop]",
        }, opts);

        new DO.SshKey("personal-laptop", new DO.SshKeyArgs
        {
            Name = "Personal Laptop",
            PublicKey = "[managed via Pulumi secret config sshKeyPersonalLaptop]",
        }, opts);
    }
}
