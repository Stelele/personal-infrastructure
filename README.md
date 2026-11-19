# personal-infrastructure

My entire personal cloud estate — DigitalOcean and Cloudflare — managed as
code in a single .NET Pulumi project, imported from live resources and
verified with `pulumi preview` → **no changes**.

## Architecture

```
                         ┌──────────────────────────────┐
                         │      Cloudflare (account)     │
                         │                              │
   giftmugweni.com ◄─────┤ zone: giftmugweni.com        │
   fountainoflifefamily.com ◄ zone: fountainoflifefamily.com
                         │   ├─ 24 managed DNS records  │
                         │   └─ 2 R2 buckets (WEUR)     │
                         │        └─ bucket.giftmugweni.com (custom domain)
                         └──────────────┬───────────────┘
                                        │ proxied / delegated DNS
                                        ▼
                         ┌──────────────────────────────┐
                         │   DigitalOcean (project)      │
                         │                              │
                         │  Droplets (lon1):            │
                         │   • ubuntu-s-1vcpu-1gb-lon1-01 (2GB, snapshot)
                         │   • internet-cafe-server     │
                         │                              │
                         │  App Platform (5 apps):      │
                         │   • personal-site   → giftmugweni.com
                         │   • voice-generation → reader.giftmugweni.com
                         │   • fountain-of-life-site → fountainoflifefamily.com
                         │   • njeremoto-dashboards → dashboard.njeremoto.net
                         │   • nbi-app                    │
                         │                              │
                         │  3 SSH keys · legacy DNS zone │
                         └──────────────────────────────┘
```

## What's managed

### DigitalOcean (`digitalocean/`)

| File | Resources |
|---|---|
| `Provider.cs` | Provider registration (token from secret stack config) |
| `SshKeys.cs` | 3 SSH keys (public keys read from encrypted stack config, not stored in the repo) |
| `Droplets.cs` | 2 Droplets in `lon1` (default VPC referenced as a constant) |
| `Apps.cs` | 5 App Platform apps (full live specs incl. alerts, domains, ingress) |
| `Dns.cs` | Legacy DNS zone `giftmugweni.com` + 9 records *(stale — see oddities)* |

### Cloudflare (`cloudflare/`)

| File | Resources |
|---|---|
| `Provider.cs` | Provider registration (scoped API token from secret stack config) |
| `Zones.cs` | 2 zones: `giftmugweni.com`, `fountainoflifefamily.com` |
| `Dns.cs` | 24 DNS records (A/AAAA/CNAME/MX/TXT, proxied flags preserved) — see oddities for what is deliberately unmanaged |
| `R2.cs` | 2 R2 buckets (WEUR) + custom domain `bucket.giftmugweni.com` |

## How it works

- **Stack**: `prod` on Pulumi Cloud (`giftmugweni-gmail-com-org/personal-infrastructure`).
- **Secrets**: never in code. Both API tokens and the DO-side encrypted
  app env blob live as encrypted stack config
  (`pulumi config set --secret …`). The repo is safe to browse.
- **Import-first**: every resource was imported from the live APIs
  (`pulumi import`, or state-injection for the one provider type that
  lacks import support). The survey dump in `docs/survey/` records exactly
  what was imported and is the audit trail.
- **Protection**: every resource carries `Protect = true` — accidental
  deletion requires an explicit `pulumi unprotect` first.
- **Acceptance gate**: after each import batch the code had to reach
  `pulumi preview` → `no changes` before it was committed. The current
  preview is **52 resources, unchanged**.

### Adding or changing a resource

1. Edit the declaration in the matching provider folder (one file per resource type).
2. `pulumi preview` — read the diff carefully; droplet-level fields like
   `image`, `name`, `size` force replacement (the resource is recreated).
3. `pulumi up` after the diff looks right.

## Known oddities

| Oddity | Why it's there |
|---|---|
| DO zone `giftmugweni.com` + 9 records is stale | The domain is delegated to Cloudflare nameservers; the DO zone is a leftover duplicate. Imported for completeness, **scheduled for deletion** in the cleanup phase. |
| `portal.giftmugweni.com` → 161.35.0.160 | Dead record pointing at a destroyed droplet. Cleanup phase. |
| `erpnext.giftmugweni.com` → 139.59.7.15 | Dead record ("erpnext integration"). Cleanup phase. |
| Droplet `ubuntu-s-1vcpu-1gb-lon1-01` is actually 2GB | Renaming a droplet recreates it, so the misnomer stays until an intentional rename window. |
| `njeremoto.net` is not here | Registered at Squarespace; DNS is served by Google Cloud DNS (`ns-cloud-e*`) with Google Workspace MX at the apex. Deliberately outside this project (owner decision 2026-11-18) — no Squarespace DNS API exists. |
| `personal-site` env `VITE_GIPHY_API_KEY` | Stored as DO-side encrypted `EV[1:…]` envelope; mirrored into Pulumi secret config so the app spec round-trips. |
| 8 CF records are unmanaged | The `bucket.giftmugweni.com` CNAME is auto-created by Cloudflare when the R2 custom domain is attached; the 3 apex NS records (pointing at DO nameservers) and 4 apex A/AAAA records (content = Cloudflare's own IPs) are migration artifacts. Left live but outside Pulumi; deletion lands in the phase-3 cleanup. |

## Getting started

```sh
dotnet build
pulumi login                # Pulumi Cloud account
pulumi stack select prod

# the six secrets (interactive):
pulumi config set --secret digitalocean:token              # DO PAT (read+write)
pulumi config set --secret cloudflare:apiToken             # CF token: R2 edit, DNS edit, zone read
pulumi config set --secret personal-infrastructure:giphyApiKey   # the EV[1:…] blob
pulumi config set --secret personal-infrastructure:sshKeyPersonalNobara    # SSH public key
pulumi config set --secret personal-infrastructure:sshKeyNewPersonalLaptop # SSH public key
pulumi config set --secret personal-infrastructure:sshKeyPersonalLaptop    # SSH public key

pulumi preview             # should report: no changes
```

**Token scope** (least privilege):
- DigitalOcean: Personal Access Token, **Read + Write**.
- Cloudflare: Custom API Token — Account → *Workers R2 Storage* (Edit),
  Zone → *DNS* (Edit, both zones), Zone → *Zone* (Read, both zones).

## License

[MIT](LICENSE) — © 2026 Gift Mugweni
