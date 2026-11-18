using Pulumi;
using Config = Pulumi.Config;
using DO = Pulumi.DigitalOcean;

namespace PersonalInfrastructure.DigitalOcean;

/// <summary>
/// The five App Platform apps, declared from their live specs (survey
/// 2026-11-18). Specs are copied field-for-field — ingress match shapes,
/// features and domains must match exactly or the import diff never settles.
/// </summary>
public static class Apps
{
    public static (
        DO.App PersonalSite,
        DO.App VoiceGeneration,
        DO.App FountainOfLifeSite,
        DO.App NjeremotoDashboards,
        DO.App NbiApp) Create(global::Pulumi.DigitalOcean.Provider provider)
    {
        var opts = new CustomResourceOptions { Provider = provider, Protect = true };

        var giphyApiKey = new Config("personal-infrastructure").RequireSecret("giphyApiKey");

        var personalSite = new DO.App("personal-site", new DO.AppArgs
        {
            DeploymentPerPage = 20,
            Spec = new DO.Inputs.AppSpecArgs
            {
                Name = "personal-site",
                Region = "blr",
                Alerts =
                {
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DEPLOYMENT_FAILED" },
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DOMAIN_FAILED" },
                },
                Domains = { "giftmugweni.com" },
                Features = { "buildpack-stack=ubuntu-22" },
                Envs =
                {
                    new DO.Inputs.AppSpecEnvArgs
                    {
                        Key = "VITE_GIPHY_API_KEY",
                        Scope = "RUN_AND_BUILD_TIME",
                        Type = "SECRET",
                        Value = giphyApiKey,
                    },
                },
                Ingress = new DO.Inputs.AppSpecIngressArgs
                {
                    Rules =
                    {
                        new DO.Inputs.AppSpecIngressRuleArgs
                        {
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = "personal-site-build" },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = "personal-site-build",
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = "Stelele/personal-site-build",
                        },
                    },
                },
            },
        }, opts);

        var voiceGeneration = new DO.App("voice-generation", new DO.AppArgs
        {
            DeploymentPerPage = 20,
            Spec = new DO.Inputs.AppSpecArgs
            {
                Name = "voice-generation",
                Region = "fra",
                Alerts =
                {
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DEPLOYMENT_FAILED" },
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DOMAIN_FAILED" },
                },
                Domains = { "reader.giftmugweni.com" },
                Features = { "buildpack-stack=ubuntu-22" },
                Ingress = new DO.Inputs.AppSpecIngressArgs
                {
                    Rules =
                    {
                        new DO.Inputs.AppSpecIngressRuleArgs
                        {
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = "voice-generation-site-built" },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = "voice-generation-site-built",
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = "Stelele/voice-generation-site-built",
                        },
                    },
                },
            },
        }, opts);

        var fountainOfLifeSite = new DO.App("fountain-of-life-site", new DO.AppArgs
        {
            DeploymentPerPage = 20,
            Spec = new DO.Inputs.AppSpecArgs
            {
                Name = "fountain-of-life-site",
                Region = "blr",
                Alerts =
                {
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DEPLOYMENT_FAILED" },
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DOMAIN_FAILED" },
                },
                Domains = { "fountainoflifefamily.com" },
                Features = { "buildpack-stack=ubuntu-22" },
                Ingress = new DO.Inputs.AppSpecIngressArgs
                {
                    Rules =
                    {
                        new DO.Inputs.AppSpecIngressRuleArgs
                        {
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = "fountain-of-life-build" },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Authority = new DO.Inputs.AppSpecIngressRuleMatchAuthorityArgs { Exact = "" },
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = "fountain-of-life-build",
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = "Stelele/fountain-of-life-build",
                        },
                    },
                },
            },
        }, opts);

        var njeremotoDashboards = new DO.App("njeremoto-dashboards", new DO.AppArgs
        {
            DeploymentPerPage = 20,
            Spec = new DO.Inputs.AppSpecArgs
            {
                Name = "njeremoto-dashboards",
                Region = "blr",
                Alerts =
                {
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DEPLOYMENT_FAILED" },
                    new DO.Inputs.AppSpecAlertArgs { Rule = "DOMAIN_FAILED" },
                },
                Domains = { "dashboard.njeremoto.net" },
                Features = { "buildpack-stack=ubuntu-22" },
                Ingress = new DO.Inputs.AppSpecIngressArgs
                {
                    Rules =
                    {
                        new DO.Inputs.AppSpecIngressRuleArgs
                        {
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = "njeremoto-dashboard-build" },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Authority = new DO.Inputs.AppSpecIngressRuleMatchAuthorityArgs { Exact = "" },
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = "njeremoto-dashboard-build",
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = "Stelele/njeremoto-dashboard-build",
                        },
                    },
                },
            },
        }, opts);

        var nbiApp = new DO.App("nbi-app", new DO.AppArgs
        {
            DeploymentPerPage = 20,
            Spec = new DO.Inputs.AppSpecArgs
            {
                Name = "nbi-app",
                Region = "ams",
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
                            Component = new DO.Inputs.AppSpecIngressRuleComponentArgs { Name = "nbi-site-build" },
                            Match = new DO.Inputs.AppSpecIngressRuleMatchArgs
                            {
                                Authority = new DO.Inputs.AppSpecIngressRuleMatchAuthorityArgs { Exact = "" },
                                Path = new DO.Inputs.AppSpecIngressRuleMatchPathArgs { Prefix = "/" },
                            },
                        },
                    },
                },
                StaticSites =
                {
                    new DO.Inputs.AppSpecStaticSiteArgs
                    {
                        Name = "nbi-site-build",
                        EnvironmentSlug = "html",
                        SourceDir = "/",
                        Github = new DO.Inputs.AppSpecStaticSiteGithubArgs
                        {
                            Branch = "main",
                            DeployOnPush = true,
                            Repo = "Stelele/nbi-site-build",
                        },
                    },
                },
            },
        }, opts);

        return (personalSite, voiceGeneration, fountainOfLifeSite, njeremotoDashboards, nbiApp);
    }
}
