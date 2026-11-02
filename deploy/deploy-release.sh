#!/usr/bin/env bash
set -Eeuo pipefail

REPO_DIR="/root/ServUO"
BRANCH="main"
REMOTE="origin"
LOCK_FILE="/run/lock/servuo-deploy.lock"
STAGING_ROOT="/tmp/servuo-deploy"

log() {
    printf '[servuo-deploy] %s\n' "$*"
}

if [[ $# -ne 1 || ! "$1" =~ ^[0-9a-f]{40}$ ]]; then
    log "ERROR: Expected one full commit SHA."
    exit 2
fi

RELEASE_SHA="$1"
ARCHIVE="$STAGING_ROOT/$RELEASE_SHA.tar.gz"
STAGING_DIR="$STAGING_ROOT/$RELEASE_SHA"

exec 9>"$LOCK_FILE"
if ! flock -n 9; then
    log "ERROR: Another deployment is already running."
    exit 1
fi

if [[ ! -e "$REPO_DIR/.git" ]]; then
    log "ERROR: Git repository not found at $REPO_DIR."
    exit 1
fi

if [[ ! -f "$ARCHIVE" ]]; then
    log "ERROR: Release archive not found at $ARCHIVE."
    exit 1
fi

log "Fetching $REMOTE/$BRANCH."
git -C "$REPO_DIR" fetch --prune "$REMOTE" "$BRANCH"
remote_head="$(git -C "$REPO_DIR" rev-parse FETCH_HEAD)"

if [[ "$remote_head" != "$RELEASE_SHA" ]]; then
    log "ERROR: Release $RELEASE_SHA is not the current $REMOTE/$BRANCH commit ($remote_head)."
    exit 1
fi

rm -rf -- "$STAGING_DIR"
mkdir -m 700 "$STAGING_DIR"
while IFS= read -r entry; do
    case "$entry" in
        ./)
            ;;
        ./*.dll|./*.exe)
            if [[ "${entry#./}" == */* ]]; then
                log "ERROR: Release archive contains an unexpected nested path: $entry."
                exit 1
            fi
            ;;
        *)
            log "ERROR: Release archive contains an unexpected path: $entry."
            exit 1
            ;;
    esac
done < <(tar -tzf "$ARCHIVE")
tar -xzf "$ARCHIVE" -C "$STAGING_DIR"

if ! git -C "$REPO_DIR" show "$RELEASE_SHA:servuo.service" > "$STAGING_DIR/servuo.service" ||
    [[ ! -s "$STAGING_DIR/servuo.service" ]]; then
    log "ERROR: Could not read servuo.service from release $RELEASE_SHA."
    exit 1
fi

for file in ServUO.exe Scripts.dll Ultima.dll; do
    if [[ ! -s "$STAGING_DIR/$file" ]]; then
        log "ERROR: Required release file is missing or empty: $file."
        exit 1
    fi
done

shopt -s nullglob
release_files=("$STAGING_DIR"/*.dll "$STAGING_DIR"/*.exe)
if [[ ${#release_files[@]} -eq 0 ]]; then
    log "ERROR: Release archive contains no assemblies."
    exit 1
fi

if systemctl cat servuo-auto-update.timer >/dev/null 2>&1; then
    log "Disabling the legacy auto-update timer."
    systemctl disable --now servuo-auto-update.timer
fi

if systemctl cat servuo-auto-update.service >/dev/null 2>&1; then
    systemctl stop servuo-auto-update.service
fi

BACKUP_DIR="$REPO_DIR/Backups/deploy/$(date -u +%Y%m%dT%H%M%SZ)-$RELEASE_SHA"
mkdir -p "$BACKUP_DIR"
existing_files=("$REPO_DIR"/*.dll "$REPO_DIR"/*.exe)
if [[ ${#existing_files[@]} -gt 0 ]]; then
    cp -a "${existing_files[@]}" "$BACKUP_DIR/"
fi

if [[ -f /etc/systemd/system/servuo.service ]]; then
    cp -a /etc/systemd/system/servuo.service "$BACKUP_DIR/servuo.service"
fi

log "Stopping ServUO."
systemctl stop servuo

for file in "${existing_files[@]}"; do
    rm -- "$file"
done
for file in "${release_files[@]}"; do
    install -m 0644 "$file" "$REPO_DIR/"
done

install -m 0644 "$STAGING_DIR/servuo.service" /etc/systemd/system/servuo.service
systemctl daemon-reload

if systemctl restart servuo && sleep 5 && systemctl is-active --quiet servuo; then
    log "Release $RELEASE_SHA deployed successfully."
    rm -rf -- "$STAGING_DIR" "$ARCHIVE"
    exit 0
fi

log "ERROR: New release failed to start; restoring previous binaries."
deployed_files=("$REPO_DIR"/*.dll "$REPO_DIR"/*.exe)
for file in "${deployed_files[@]}"; do
    rm -- "$file"
done
backup_files=("$BACKUP_DIR"/*.dll "$BACKUP_DIR"/*.exe)
if [[ ${#backup_files[@]} -gt 0 ]]; then
    cp -a "${backup_files[@]}" "$REPO_DIR/"
fi

if [[ -f "$BACKUP_DIR/servuo.service" ]]; then
    install -m 0644 "$BACKUP_DIR/servuo.service" /etc/systemd/system/servuo.service
    systemctl daemon-reload
fi

if ! systemctl restart servuo; then
    log "CRITICAL: Rollback restart failed; inspect systemctl status servuo."
fi

exit 1
