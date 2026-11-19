using System.Collections.Generic;
using Pulumi;
using Config = Pulumi.Config;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// App Platform apps, declared as data. Adding an app is one entry in
/// <see cref="Catalog"/> — the loop below builds the full spec from the live
/// shape every app shares (two alerts, one static site from GitHub, single
/// ingress rule to that site, optional primary domain, optional env vars).
/// Returns apps keyed by their live name.
/// </summary>
public static class Apps
{
    private sealed record AppDef(
        string Name,
        string Region,
        string Repo,
        string StaticSiteName,
        string Domain,          // "" = no custom domain
        string? DomainZone,     // zone name for the domain (only personal-site carries one)
        bool AuthorityExact,    // ingress match.authority.exact: ""
        (string Key, string Scope, string Type, string SecretKey)? EnvSecret = null);

    private static readonly AppDef[] Catalog =
    {
        new("personal-site", "blr", "Stelele/personal-site-build", "personal-site-build",
            "giftmugweni.com", "giftmugweni.com", AuthorityExact: false,
            EnvSecret: ("VITE_GIPHY_API_KEY", "RUN_AND_BUILD_TIME", "SECRET", "giphyApiKey")),
        new("voice-generation", "fra", "Stelele/voice-generation-site-built", "voice-generation-site-built",
            "reader.giftmugweni.com", null, AuthorityExact: false),
        new("fountain-of-life-site", "blr", "Stelele/fountain-of-life-build", "fountain-of-life-build",
            "fountainoflifefamily.com", null, AuthorityExact: true),
        new("njeremoto-dashboards", "blr", "Stelele/njeremoto-dashboard-build", "njeremoto-dashboard-build",
            "dashboard.njeremoto.net", null, AuthorityExact: true),
        new("nbi-app", "ams", "Stelele/nbi-site-build", "nbi-site-build",
            "", null, AuthorityExact: true),
    };

    public static Dictionary<string, DO.App> Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };
        var config = new Config("personal-infrastructure");
        var apps = new Dictionary<string, DO.App>();

        foreach (var app in Catalog)
        {
            var spec = new DO.Inputs.AppSpecArgs
            {
                Name = app.Name,
                Region = app.Region,
                Alerts =
                {
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DEPLOYMENT_FAILED" },
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DOMAIN_FAILED" },
                },
                Features = { "buildpack-stack=ubuntu-22" },
                Ingress = new DO.Inputs.AppSpecIngressArgs
                {
                    Rules =
                    {
                        new DO.Inputs.AppSpecIngressRuleArgs
                        {
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = app.StaticSiteName },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Authority = app.AuthorityExact
                                    ? new DO.Inputs.AppSpecIngressRuleMatchAuthorityArgs { Exact = "" }
                                    : null,
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = app.StaticSiteName,
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = app.Repo,
                        },
                    },
                },
            };

            if (app.Domain.Length > 0)
            {
                spec.Domains = new Pulumi.InputList<string> { app.Domain };
            }
            if (app.EnvSecret is { } env)
            {
                spec.Envs = new Pulumi.InputList<DO.Inputs.AppSpecEnvArgs>
                {
                    new DO.Inputs.AppSpecEnvArgs
                    {
                        Key = env.Key,
                        Scope = env.Scope,
                        Type = env.Type,
                        Value = config.RequireSecret(env.SecretKey),
                    },
                };
            }

            apps[app.Name] = new DO.App(app.Name, new DO.AppArgs
            {
                // Provider-local default (not part of the DO spec); see ledger.
                DeploymentPerPage = 20,
                Spec = spec,
            }, opts);
        }

        return apps;
    }
}
