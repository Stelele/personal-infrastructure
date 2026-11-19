using Pulumi;
using Config = Pulumi.Config;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// SSH keys registered with DigitalOcean. Public keys are not secret, but per
/// owner decision 2026-11-19 they live in encrypted stack config (not in this
/// public repo): set each with `pulumi config set --secret
/// personal-infrastructure:sshKey&lt;Name&gt;`.
/// </summary>
public static class SshKeys
{
    private static readonly (string LogicalName, string Name, string ConfigKey)[] Keys =
    {
        ("personal-nobara-machine", "Personal Nobara Machine", "sshKeyPersonalNobara"),
        ("new-personal-laptop", "New Personal Laptop", "sshKeyNewPersonalLaptop"),
        ("personal-laptop", "Personal Laptop", "sshKeyPersonalLaptop"),
    };

    public static void Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };
        var config = new Config("personal-infrastructure");

        foreach (var (logicalName, name, configKey) in Keys)
        {
            new DO.SshKey(logicalName, new DO.SshKeyArgs
            {
                Name = name,
                PublicKey = config.RequireSecret(configKey),
            }, opts);
        }
    }
}
