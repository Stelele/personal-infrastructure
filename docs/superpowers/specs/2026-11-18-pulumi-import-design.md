# Pulumi Import — Design

**Date:** 2026-11-18 · **Status:** draft → builder check

---

## Intent

Every existing DigitalOcean and Cloudflare resource becomes visible in one Pulumi
program, stored in Pulumi Cloud state. The repo is **public** and doubles as a
technical showcase, so the code must read like a map: open a folder, see what's what.

```
Why now ────────────────────────────────────────────────┐
"Slowly adding bits and pieces… I'll eventually        │
 forget something I did and it'll bite me in the bum." │
                                                       ▼
        ┌──────────────────┐         ┌──────────────────────────┐
        │ Pulumi Cloud UI  │◀──state─┤ personal-infrastructure  │
        │ (the live map)   │         │ (the readable source)    │
        └──────────────────┘         └──────────────────────────┘
```

---

## Goals / Non-goals / Anti-goals

| | |
|---|---|
| **Goal** | Import every existing DO + Cloudflare resource into one Pulumi project |
| **Goal** | Per-provider folders, per-resource-type files — readable at a glance |
| **Goal** | `pulumi preview` reports **no changes** when the import is faithful |
| **Goal** | Repo is safe to be public: no secrets in code, showcase-quality README |
| **Non-goal** | Creating new infrastructure (that comes later, on top of this) |
| **Non-goal** | CI/preview pipelines (candidate for a later phase) |
| **Non-goal** | Managing droplet backups/snapshots via code — they stay console-managed |
| **Anti-goal** | A sprawling multi-stack/multi-project layout — this is one project, one stack |
| **Anti-goal** | Importing provider-managed furniture (DO default VPCs) as "resources" |

---

## Constraints

| Constraint | Consequence |
|---|---|
| Repo will be **public** | Zero secrets in source; tokens via Pulumi secret config only |
| Language is **.NET** (template already exists) | C# program, `Pulumi.DigitalOcean` + `Pulumi.Cloudflare` providers |
| Backend is **Pulumi Cloud** (CLI already authed) | State encrypted at rest; free tier is sufficient |
| One project, one stack | Stack name: `prod` — these resources *are* live production *(my call)* |
| Stack `prod` already exists | Org `giftmugweni-gmail-com-org`, project `personal-infrastructure`; state is empty (never deployed) — import starts from a clean state |
| Import-then-clean agreed | Dead/legacy resources are imported first, deleted in a later reviewed change |

---

## Repo layout

```
personal-infrastructure/
├── Pulumi.yaml
├── Program.cs                      ← entry: calls each module, exports outputs
├── cloudflare/
│   ├── Provider.cs                 ← provider registration + config reads
│   ├── Zones.cs                    ← 2 zones (giftmugweni.com, fountainoflifefamily.com)
│   ├── Dns.cs                      ← 32 records across the 2 zones
│   └── R2.cs                       ← 2 buckets (apps-bucket, public-apps-bucket)
├── digitalocean/
│   ├── Provider.cs
│   ├── Droplets.cs                 ← 2 droplets
│   ├── Apps.cs                     ← 5 App Platform apps (full live specs)
│   ├── SshKeys.cs                  ← 3 keys
│   └── Dns.cs                      ← DO zone + its records (incl. legacy ones)
└── README.md                       ← the showcase: diagram + how it works
```

`Program.cs` stays a thin orchestrator; every resource lives in the provider folder.
One file per resource *type*, not per resource.

---

## Inventory (what gets imported)

### DigitalOcean

| Resource | Count | Notes |
|---|---|---|
| Droplets | 2 | `ubuntu-s-1vcpu-1gb-lon1-01` (167.99.93.239 — misnamed, actually 2 GB), `internet-cafe-server` (165.232.33.196). First boots from a **custom snapshot** (image `194230265`) — imported with the snapshot ID. Both droplets sit in the *default* lon1 VPC (`11799a23-…`): the VPC is not imported, the UUID is referenced as a plain constant so there is no perpetual diff. |
| App Platform apps | 5 | personal-site → giftmugweni.com · voice-generation → reader.giftmugweni.com · fountain-of-life-site → fountainoflifefamily.com · njeremoto-dashboards → dashboard.njeremoto.net · nbi-app (domainless — serves its `*.ondigitalocean.app` URL). All build from `Stelele/*` GitHub repos with deploy-on-push. `personal-site` carries one SECRET env var (`VITE_GIPHY_API_KEY`) — supplied via Pulumi secret config, never inline (see hygiene). Full live specs captured in the survey dump. |
| SSH keys | 3 | public keys are public by nature — safe inline |
| DNS domain + records | 1 zone, 9 records | includes 3 legacy NS records + stale apex copies (see Cleanup) |

### Cloudflare

| Resource | Count | Notes |
|---|---|---|
| Zones | 2 | both on Free plan |
| DNS records | 32 | 30 in giftmugweni.com, 2 in fountainoflifefamily.com |
| R2 buckets | 2 | `apps-bucket`, `public-apps-bucket` (WEUR, Standard). The public one is exposed via the `bucket.giftmugweni.com` CNAME (already in the DNS import) **plus a real R2 Custom Domain binding** — imported as its own resource so the pairing stays intact |

### Explicitly excluded

| Resource | Why |
|---|---|
| 3 default VPCs (lon1, nyc1, blr1) | auto-created by DO, not user intent; droplet code references the lon1 VPC by UUID constant instead of importing it |
| Droplet backups / snapshots | not Pulumi resources; noted in README as console-managed |
| Cloudflare universal SSL certificates (1/zone) | auto-issued and rotated by Cloudflare — provider furniture |
| Cloudflare managed free ruleset | created and maintained by Cloudflare's security team — provider furniture |

**Exhaustiveness:** every DO and Cloudflare resource type available to the account was
listed on 2026-11-18. Empty results: volumes, load balancers, firewalls, container
registries, alert rules, Spaces CDNs, managed databases, Kubernetes clusters, functions,
vector databases; Pages, Workers, D1, KV, tunnels, email routing rules, worker routes.
Raw survey data (IDs + full specs): [`docs/survey/2026-11-18/`](../../survey/2026-11-18/).

---

## Import mechanics

```
Survey (done, via MCP APIs) ──▶ Write C# declarations ──▶ pulumi import <type> <name> <id>
        │                          (from survey data)           │ binds live ID into state
        │                                                       ▼
        │                                          paste generated code into files
        ▼                                                       │
  real resource IDs ──────────────────────────────▶ pulumi preview == "no changes" ✅
```

- **Acceptance criterion:** after each batch, `pulumi preview` shows *no changes*.
  Any diff = an unfaithful declaration. Fix before moving on.
- Import commands are run resource-by-resource or as batches; exact
  `pulumi import` usage for C# is verified against current Pulumi docs at build
  time — not from memory (board rule: check the real docs).
- Provider tokens come from `pulumi config set --secret` (or env vars during
  import runs). They never appear in source, and Pulumi Cloud state encrypts them.

---

## Public-repo hygiene

| Concern | Decision |
|---|---|
| API tokens (DO + CF) | `pulumi config` secrets / env vars — never in code or git |
| SECRET env vars in app specs (e.g. `personal-site`'s `VITE_GIPHY_API_KEY`) | supplied via `pulumi config set --secret`; app spec in code references the config value, never the encrypted blob |
| Domains, IPs, DNS contents, SSH public keys | inline literals — DNS is public data; inline is what makes the map readable |
| Personal email in SSH key comments | left as-is (part of the public keys themselves) |
| App Platform specs (repo refs, build config) | inline — repo names are already public via the apps' URLs |
| README | written as the showcase piece: one architecture diagram, one table per provider, a "how this works" section, and a "known oddities" note |
| State itself | lives on Pulumi Cloud (private), not in the repo |

---

## Known oddities (imported, cleaned up later)

| # | Finding | Phase-2 action |
|---|---|---|
| 1 | `portal.giftmugweni.com` → 161.35.0.160 — IP belongs to a deleted droplet | delete record |
| 2 | `erpnext.giftmugweni.com` → 139.59.7.15 — no droplet at that IP | confirm + delete |
| 3 | DO DNS zone `giftmugweni.com` — stale duplicate, domain delegated to Cloudflare NS | delete zone (after confirming nothing resolves from it) |
| 4 | `dashboard.njeremoto.net` has no DNS zone in DO or CF | **Owner decision 2026-11-18: leave it.** Registered at Squarespace, DNS still served by Google Cloud DNS (`ns-cloud-e*.googledomains.com`) with Google Workspace MX/SPF at the apex. No Squarespace DNS API exists and the official Squarespace MCP is domain-search only — note in README, out of IaC scope |
| 5 | First droplet named `ubuntu-s-1vcpu-1gb-lon1-01` but is 2 GB | rename or leave; document |

---

## Safety model

| Layer | Mechanism |
|---|---|
| Accidental deletion | `protect: true` on droplets, apps, zones, buckets *(my call)* — removed deliberately when a teardown is wanted |
| Drift detection | routine `pulumi refresh` + `preview` |
| State history | Pulumi Cloud keeps full history and stacks |
| Destructive ops | `pulumi destroy` remains an explicit manual decision, never in automation |

---

## Phases

| Phase | Content | Done when |
|---|---|---|
| 1 | Providers + config, droplets + SSH keys, apps, DO DNS, CF zones/DNS/R2 — imported, all previews clean | `pulumi preview` → no changes; committed to git |
| 2 | README showcase rewrite + MIT LICENSE | a stranger can read the repo and say what you run and why |
| 3 | Cleanup PR: delete oddities 1–3 above | reviewed diff, still preview-clean afterwards |---

## Left to the builder

- Exact `pulumi import` invocation pattern for C# (verify against current docs)
- Module wiring style in `Program.cs` (static methods vs. classes) — any clean, obvious choice
- Where provider config values are read (config namespace conventions)
- Order of imports within phase 1 (no hard dependencies exist between resources)
