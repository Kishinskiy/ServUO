# GitHub Actions deployment

The `ServUO CI/CD` workflow builds the `Release|x64` solution on an Ubuntu runner with the .NET SDK. The projects target .NET Framework 4.8, so the resulting assemblies run on the Linux host through Mono. Pull requests build and validate the solution but do not deploy. A successful push to `main` uploads the built assemblies, fast-forwards the existing checkout at `/root/ServUO`, installs the release, and restarts the `servuo` systemd service. `workflow_dispatch` can deploy the current `main` commit manually.

The server keeps its existing `Config`, `Data`, `Saves`, and other runtime files. The release includes `ServUO.exe`, `Scripts.dll`, `Ultima.dll`, and all other top-level DLL/EXE build outputs (including runtime dependencies such as SQLite). Existing top-level DLL/EXE files and the service unit are backed up under `/root/ServUO/Backups/deploy/` before activation. If the new service fails to start, the deployment script restores those files and attempts to restart the previous release.

## GitHub setup

Create the `production` environment in the repository's **Settings → Environments**. Add these environment secrets:

- `DEPLOY_SSH_KEY`: private key authorized for `root@kishinskiy.ru`.
- `DEPLOY_KNOWN_HOSTS`: the verified `known_hosts` entry for `kishinskiy.ru` (obtain and verify the host fingerprint through a trusted channel).

Do not put either key or host fingerprint in the repository. The SSH public key must be installed in `/root/.ssh/authorized_keys` on the host. The server checkout at `/root/ServUO` must be on `main`, have a clean tracked worktree, and be able to fetch `origin/main`.

The first successful deployment installs the repository's `servuo.service` unit, which runs the prebuilt release through Mono, and disables the legacy `servuo-auto-update.timer` so it cannot race with Actions. It uses `systemctl`, `screen`, and `mono` already required by the existing service setup.
