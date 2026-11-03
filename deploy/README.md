# GitHub Actions deployment

The `ServUO CI/CD` workflow builds the `Release|x64` solution on an Ubuntu runner with the .NET SDK. The projects target .NET Framework 4.8, so the resulting assemblies run on the Linux host through Mono. Pull requests build and validate the solution but do not deploy. A successful push to `main` packages the complete tracked server tree (`Config`, `Data`, `Scripts`, `Server`, `Ultima`, `Spawns`, runtime dependencies, and the systemd unit) together with the freshly built binaries. It can install onto a new host without a pre-existing Git checkout. `workflow_dispatch` can deploy the current `main` commit manually.

The release is a snapshot of files tracked by Git plus build outputs; `.git` and ignored runtime state are not packaged. Deployment overlays the release onto `/root/ServUO`, so runtime data already on a host (including `Saves`, `Logs`, and backups) is preserved. On a new host, the server starts with fresh runtime state; migrating existing player/world saves requires a separate transfer. Tracked `Config`, `Data`, and `Spawns` files are copied on every deployment. ServUO restarts only when a top-level DLL/EXE binary changes or is removed; config, data, source, spawn XML, and service-unit-only updates do not restart it. Existing top-level binaries and the service unit are backed up under `/root/ServUO/Backups/deploy/` before a binary update. If the new service fails to start, the deployment script restores the previous binaries and attempts to restart the previous release.

## GitHub setup

Create the `production` environment in the repository's **Settings → Environments**. Add these environment secrets:

- `DEPLOY_SSH_KEY`: private key authorized for `root@kishinskiy.ru`.
- `DEPLOY_KNOWN_HOSTS`: the verified `known_hosts` entry for `kishinskiy.ru` (obtain and verify the host fingerprint through a trusted channel).

Do not put either key or host fingerprint in the repository. The SSH public key must be installed in `/root/.ssh/authorized_keys` on the host. The deploy user must be able to install files under `/root/ServUO`, manage systemd, and run the existing required server runtime (Mono and screen).

The first successful deployment installs the repository's `servuo.service` unit, which runs the prebuilt release through Mono, and disables the legacy `servuo-auto-update.timer` so it cannot race with Actions. It uses `systemctl`, `screen`, and `mono` on the Linux host.
