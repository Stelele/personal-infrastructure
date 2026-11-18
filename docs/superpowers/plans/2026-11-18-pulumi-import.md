# Pulumi Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Import every existing DigitalOcean and Cloudflare resource into the single .NET Pulumi project so `pulumi preview` reports no changes, with per-provider / per-resource-type files.

**Architecture:** One Pulumi project, one stack (`prod`, already created and empty on Pulumi Cloud). Two provider resources (DigitalOcean, Cloudflare) registered first; each subsequent task writes resource declarations from the survey dump, runs `pulumi import` to bind live IDs into state, and proves fidelity with a clean `pulumi preview`. The repo is bootstrapped as a git repo, README + MIT license added, then pushed to a new public GitHub repo with a PR.

**Tech Stack:** .NET / C# (existing template), `Pulumi.DigitalOcean`, `Pulumi.Cloudflare` NuGet packages, Pulumi Cloud backend, GitHub CLI for publishing.

**Spec:** `docs/superpowers/specs/2026-11-18-pulumi-import-design.md` (read alongside; the inventory and hygiene decisions live there)

## Global Constraints

- Survey data (IDs, specs, record values) comes from `docs/survey/2026-11-18/{digitalocean,cloudflare}.json` — never re-guess values.
- Tokens only via `pulumi config set --secret`; the `EV[1:...]` blob and both API tokens must never appear in code or git history.
- Import with `--protect` (default true) on every resource.
- Import ID formats not listed in the survey must be read from the resource's **Import** section in the Pulumi Registry before importing (board rule: check real docs).
- Stack: `prod` at `giftmugweni-gmail-com-org/personal-infrastructure` — `pulumi stack select prod` first.
- Acceptance for every import task: `pulumi preview` → "no changes".
- C# naming: namespaces/classes PascalCase, parameters camelCase, file name matches type.
- `docs/survey/` stays in the repo (it is public data: DNS records, public keys) — it is the audit trail of what was imported.

## Review Focus

1. **Secret leakage** — the `VITE_GIPHY_API_KEY` EV blob and API tokens: a stranger browsing the public repo must find nothing. Test: `git grep -E "EV\[1:|DIGITALOCEAN_TOKEN|CLOUDFLARE_API_TOKEN"` returns nothing and `git log -p | grep -c "EV\[1:"` returns 0 — pinned in Task 5.
2. **TXT record exact content** — the survey content strings include the surrounding double quotes (e.g. `"v=DMARC1; p=none;"`); a dropped or doubled quote breaks DNS semantics and shows as a diff. Test: preview clean after the record batch — pinned in Task 6.
3. **Proxied flag fidelity** — `api.erp-dashboard` and `portal` records are `proxied: false` while their siblings are true; flipping one silently changes traffic paths. Test: preview clean — pinned in Task 6.
4. **App spec completeness** — each app must carry its full live spec (`alerts`, `domains`, `features`, ingress match shapes) or the import diff never settles. Test: preview clean per app — pinned in Task 5.
5. **Wrong import-ID format** — record-type resources use compound IDs (`<zone>/<record>`); a wrong format fails the import outright. Test: the check-the-docs step before each batch — Tasks 3, 4, 6, 7.

---

### Task 1: Git bootstrap

**Files:**
- Create: `.gitignore` already exists (verify it covers `bin/`, `obj/`)

**Interfaces:**
- Produces: repo on branch `feat/pulumi-import`; `main` holds only the scaffold commit.

- [ ] **Step 1: Init and scaffold commit**

```bash
cd /home/gift/Documents/code-projects/personal-infrastructure
git init -b main
git add .gitignore Pulumi.yaml personal-infrastructure.csproj Program.cs docs/
git commit -m "chore: initialize pulumi dotnet project with import design and survey data"
```

- [ ] **Step 2: Verify .gitignore covers build output**

Run: `git check-ignore bin obj && echo OK`
Expected: `OK`

- [ ] **Step 3: Feature branch**

```bash
git checkout -b feat/pulumi-import
```

### Task 2: Providers + config wiring

**Files:**
- Create: `digitalocean/Provider.cs`, `cloudflare/Provider.cs`
- Modify: `Program.cs`
- Modify: `personal-infrastructure.csproj` (add `Pulumi.DigitalOcean`, `Pulumi.Cloudflare`)

**Interfaces:**
- Consumes: config keys `digitalocean:token`, `cloudflare:apiToken` (set as secrets in Step 2).
- Produces: `PersonalInfrastructure.DigitalOcean.ProviderFactory.Create()` and `PersonalInfrastructure.Cloudflare.ProviderFactory.Create()`, each returning the provider instance; Program.cs holds them in variables `doProvider` / `cfProvider` and passes them to later `Create` calls.

- [ ] **Step 1: Add packages**

```bash
dotnet add package Pulumi.DigitalOcean
dotnet add package Pulumi.Cloudflare
```

- [ ] **Step 2: Set secret config**

```bash
pulumi stack select prod
pulumi config set --secret digitalocean:token
pulumi config set --secret cloudflare:apiToken
```

- [ ] **Step 3: Write both provider factories**

Each file is a static class with one method, provider name `do` / `cf`:

```csharp
public static Provider Create() =>
    new("do", new ProviderArgs { Token = new Config("digitalocean").RequireSecret("token") });
```

- [ ] **Step 4: Wire Program.cs to instantiate both providers** (no resources yet; return empty outputs dict)

- [ ] **Step 5: Build + deploy providers**

Run: `dotnet build && pulumi up -y`
Expected: 2 provider resources created, preview shows nothing else.

- [ ] **Step 6: Capture provider URNs for later import commands**

```bash
pulumi stack export | jq -r '.deployment.resources[] | select(.type=="pulumi:providers:digitalocean") | .urn'
pulumi stack export | jq -r '.deployment.resources[] | select(.type=="pulumi:providers:cloudflare") | .urn'
```

Save both URNs in the task notes; every later import uses `--provider "do=<urn>"` / `--provider "cf=<urn>"`.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: register digitalocean and cloudflare providers"
```

### Task 3: DigitalOcean SSH keys + droplets

**Files:**
- Create: `digitalocean/SshKeys.cs`, `digitalocean/Droplets.cs`
- Modify: `Program.cs` (call both `Create` methods)

**Interfaces:**
- Consumes: `ProviderFactory.Create()` (Task 2).
- Produces: `SshKeys.Create(Provider)` → void; `Droplets.Create(Provider)` → `(Droplet UbuntuLon1, Droplet InternetCafeServer)`. Resource names: `personal-nobara-machine`, `new-personal-laptop`, `personal-laptop`, `ubuntu-s-1vcpu-1gb-lon1-01`, `internet-cafe-server`.

- [ ] **Step 1: Check import sections** in the Registry for `digitalocean:index/sshKey.SshKey` and `digitalocean:index/droplet.Droplet` (import ID format + required properties).

- [ ] **Step 2: Write declarations from the survey file** — droplet one: `Size = "s-1vcpu-2gb"`, `Image = 194230265` (snapshot), `Region = "lon1"`, `VpcUuid = "11799a23-7c4c-4f53-9c4b-b027138bf246"` (constant — default VPC is deliberately not imported), tags, ipv6, monitoring. Droplet two: `Size = "s-1vcpu-1gb"`, base image `ubuntu-24-04-x64`, same VPC constant, tags.

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: success.

- [ ] **Step 4: Import the five resources**

```bash
pulumi import 'digitalocean:index/sshKey.SshKey' personal-nobara-machine 55949597 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/sshKey.SshKey' new-personal-laptop 49471038 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/sshKey.SshKey' personal-laptop 46280680 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/droplet.Droplet' ubuntu-s-1vcpu-1gb-lon1-01 484108717 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/droplet.Droplet' internet-cafe-server 572729769 --provider "do=$DO_URN" -y
```

- [ ] **Step 5: Preview must be clean**

Run: `pulumi preview`
Expected: `no changes`. Fix any arg mismatch in the declarations and re-run until clean.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: import digitalocean ssh keys and droplets"
```

### Task 4: DigitalOcean DNS (domain + 9 records)

**Files:**
- Create: `digitalocean/Dns.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `ProviderFactory.Create()` (Task 2).
- Produces: `Dns.Create(Provider)` → void. Resource names: domain `giftmugweni-com`; records `do-record-apex-soa`, `do-record-apex-ns-1`…`ns-3`, `do-record-apex-a-cf-1/2`, `do-record-apex-aaaa-cf-1/2/3`, `do-record-api-a`.

- [ ] **Step 1: Check import sections** for `digitalocean:index/domain.Domain` and `digitalocean:index/domainRecord.DomainRecord` (record import ID is likely `<domain>/<record_id>` — confirm).

- [ ] **Step 2: Write declarations** — domain + a small static list of the 9 records from the survey file's `dns.records`, looped into `DomainRecord` resources. This zone is documented as legacy/stale (cleanup phase 3 deletes it) — a header comment says exactly that.

- [ ] **Step 3: Build** — `dotnet build`, expect success.

- [ ] **Step 4: Import** — one `pulumi import 'digitalocean:index/domain.Domain' giftmugweni-com giftmugweni.com ...`, then the 9 record imports using the confirmed ID format.

- [ ] **Step 5: Preview clean** — `pulumi preview` → `no changes`; fix mismatches until clean.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: import digitalocean dns zone and records"
```

### Task 5: DigitalOcean App Platform apps

**Files:**
- Create: `digitalocean/Apps.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `ProviderFactory.Create()` (Task 2); new secret config key `personal-infrastructure:giphyApiKey` (set in Step 2, holding the live `EV[1:...]` blob fetched in Step 1).
- Produces: `Apps.Create(Provider)` → `(App PersonalSite, App VoiceGeneration, App FountainOfLifeSite, App NjeremotoDashboards, App NbiApp)`. Resource names equal the app names.

- [ ] **Step 1: Fetch the live spec of `personal-site`** (app id `c5b54ded-4097-49d2-a776-dc2747096264`) and copy the `EV[1:...]` value of `VITE_GIPHY_API_KEY` — into the command below only, never into a file.

- [ ] **Step 2: Store it as a secret**

```bash
pulumi config set --secret personal-infrastructure:giphyApiKey   # paste EV blob when prompted
```

- [ ] **Step 3: Write the five app declarations** from the survey file's `apps[].spec` — full specs: `alerts`, `domains` (nbi-app has none), `features`, `ingress` (keep the `authority.exact: ""` shapes exactly as captured), `static_sites` with GitHub repo/branch/deploy_on_push. `personal-site`'s env entry: `Key="VITE_GIPHY_API_KEY"`, `Type="SECRET"`, `Scope="RUN_AND_BUILD_TIME"`, `Value = new Config("personal-infrastructure").RequireSecret("giphyApiKey")`.

- [ ] **Step 4: Build** — `dotnet build`, expect success.

- [ ] **Step 5: Import the five apps**

```bash
pulumi import 'digitalocean:index/app.App' personal-site c5b54ded-4097-49d2-a776-dc2747096264 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/app.App' voice-generation 27064ad1-4a12-468b-85ff-1fef34e519a7 --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/app.App' fountain-of-life-site daa7e038-19ef-4162-a713-dd278362fd6f --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/app.App' njeremoto-dashboards 4ac65010-28b6-4851-876c-f273cdc7fead --provider "do=$DO_URN" -y
pulumi import 'digitalocean:index/app.App' nbi-app 52b6d77d-baf5-474b-a5b8-276f35e61021 --provider "do=$DO_URN" -y
```

- [ ] **Step 6: Preview clean** — `pulumi preview` → `no changes`; fix spec mismatches until clean.

- [ ] **Step 7: Secret-leak check**

Run: `git grep -E 'EV\[1:' ; git log -p --all | grep -c 'EV\[1:'`
Expected: no matches in working tree; `0` in history.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: import digitalocean app platform apps"
```

### Task 6: Cloudflare zones + DNS (2 zones, 32 records)

**Files:**
- Create: `cloudflare/Zones.cs`, `cloudflare/Dns.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `ProviderFactory.Create()` (Task 2).
- Produces: `Zones.Create(Provider)` → `(Zone GiftMugweniCom, Zone FountainOfLifeFamily)`, resource names `giftmugweni-com` / `fountainoflifefamily-com`; `Dns.Create(Provider)` → void, record names `cf-record-<zone>-<name>-<type>` with `-2`/`-3` suffixes where a name+type repeats (e.g. two apex A records).

- [ ] **Step 1: Check import sections** for `cloudflare:index/zone.Zone` (ID = zone id) and `cloudflare:index/record.Record` (ID = `<zone_id>/<record_id>` — confirm in the Registry).

- [ ] **Step 2: Write zone declarations** — `Name`, `AccountId = "07e0cf4e7092208301d9db7ca03e3a77"` (inline constant; it is an identifier, not a secret), `Type = "full"`.

- [ ] **Step 3: Write the 32 record declarations** as a static list looped over `Record` resources — `ZoneId` passed as the literal zone id string, `Type`, `Name`, `Content`, `Proxied`, `Ttl = 1` (= auto) or the survey value, `Priority` for the MX record, `Comment` where present. TXT content copied byte-for-byte from the survey (quotes included).

- [ ] **Step 4: Build** — `dotnet build`, expect success.

- [ ] **Step 5: Import** — the 2 zones, then all 32 records with the confirmed `<zone_id>/<record_id>` format.

- [ ] **Step 6: Preview clean** — `pulumi preview` → `no changes`; fix mismatches until clean.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: import cloudflare zones and dns records"
```

### Task 7: Cloudflare R2 (2 buckets + custom domain)

**Files:**
- Create: `cloudflare/R2.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `ProviderFactory.Create()` (Task 2).
- Produces: `R2.Create(Provider)` → void. Resource names: `apps-bucket`, `public-apps-bucket`, `public-apps-bucket-custom-domain`.

- [ ] **Step 1: Check import sections** for `cloudflare:index/r2Bucket.R2Bucket` (ID = bucket name) and `cloudflare:index/r2CustomDomain.R2CustomDomain` (ID format likely `<account_id>/<bucket>/<domain>` — confirm).

- [ ] **Step 2: Write declarations** — buckets with `Location` (WEUR, from survey) and `StorageClass`; custom domain binding with `Bucket`, `Name = "bucket.giftmugweni.com"`, `ZoneId = "b5ddc47f934ad6a572fba57177c9987f"`, `Enabled = true`, `MinTls = "1.0"`.

- [ ] **Step 3: Build** — `dotnet build`, expect success.

- [ ] **Step 4: Import** — 2 buckets, then the custom domain.

- [ ] **Step 5: Preview clean** — `pulumi preview` → `no changes`; fix until clean.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: import cloudflare r2 buckets and custom domain"
```

### Task 8: Stack outputs + full verification

**Files:**
- Modify: `Program.cs`

**Interfaces:**
- Consumes: tuple returns from Tasks 3, 5, 6.
- Produces: stack outputs `dropletUbuntuIp`, `dropletInternetCafeIp`, `appPersonalSiteUrl`, `appVoiceGenerationUrl`, `appFountainOfLifeUrl`, `appNjeremotoDashboardUrl`, `appNbiAppUrl`, `zoneGiftmugweniId`, `zoneFountainOfLifeId`.

- [ ] **Step 1: Add the outputs dictionary** from the tuple-returned resources (`Ips`, `Url` properties on droplets/apps; `Id` on zones).

- [ ] **Step 2: Full-stack proof**

Run: `dotnet build && pulumi preview`
Expected: `no changes` across all ~50 resources. If anything diffs, fix and re-run — this is the phase-1 acceptance gate from the spec.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: export stack outputs and verify full import"
```

### Task 9: README + MIT license

**Files:**
- Create: `README.md`, `LICENSE`

**Interfaces:**
- Produces: the public-facing repo surface. MIT license holder line: `Copyright (c) 2026 Gift Mugweni` — confirm the name with the human partner during handoff if it looks wrong.

- [ ] **Step 1: Write README.md** — sections: one ASCII architecture diagram (what runs where: droplets, apps, DNS zones, R2); one table per provider from the spec inventory; "How this works" (providers, stack, preview-clean workflow, how to add a resource); "Known oddities" (the 5 from the spec, including: portal/erpnext dead records pending cleanup, stale DO zone pending cleanup, droplet misname, njeremoto.net registered at Squarespace + DNS on Google Cloud DNS + Google Workspace MX — deliberately outside IaC, owner decision 2026-11-18); "Getting started" (dotnet, pulumi login, `pulumi config set --secret` for the three secret keys, `pulumi preview`).

- [ ] **Step 2: Write LICENSE** — standard MIT text.

- [ ] **Step 3: Commit**

```bash
git add README.md LICENSE && git commit -m "docs: add readme showcase and mit license"
```

### Task 10: Publish to GitHub + PR

**Files:** none (remote operations)

**Interfaces:**
- Produces: public GitHub repo `giftmugweni/personal-infrastructure` (or the CLI-chosen owner), branch `feat/pulumi-import` pushed, PR opened against `main`.

- [ ] **Step 1: Create the public repo and push the bootstrap branch**

```bash
gh repo create personal-infrastructure --public --source . --push
```

This pushes local `main` (the single scaffold commit) as repo bootstrap, then push the feature branch:

```bash
git push -u origin feat/pulumi-import
```

- [ ] **Step 2: Open the PR**

```bash
gh pr create --fill
```

- [ ] **Step 3: Report back** — merging into `main` requires the human partner's explicit instruction (house rule). Report the PR URL and stop.
