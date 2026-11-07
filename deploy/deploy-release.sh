#!/usr/bin/env bash
set -Eeuo pipefail

REPO_DIR="$HOME/ServUO"
LOCK_FILE="$HOME/.servuo-deploy.lock"
STAGING_ROOT="$HOME/tmp/servuo-deploy"

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

if [[ ! -f "$ARCHIVE" ]]; then
    log "ERROR: Release archive not found at $ARCHIVE."
    exit 1
fi

rm -rf -- "$STAGING_DIR"
mkdir -m 700 "$STAGING_DIR"
while IFS= read -r entry; do
    case "$entry" in
        ./)
            ;;
        ./*)
            relative_path="${entry#./}"
            case "$relative_path" in
                ""|/*|..|../*|*/..|*/../*)
                    log "ERROR: Release archive contains an unsafe path: $entry."
                    exit 1
                    ;;
            esac
            ;;
        *)
            log "ERROR: Release archive contains an unexpected path: $entry."
            exit 1
            ;;
    esac
done < <(tar -tzf "$ARCHIVE")
if ! tar -tvzf "$ARCHIVE" | awk '$1 !~ /^[-d]/ { invalid = 1 } END { exit invalid }'; then
    log "ERROR: Release archive contains a link or unsupported file type."
    exit 1
fi
tar -xzf "$ARCHIVE" -C "$STAGING_DIR" --no-same-owner --no-same-permissions

for directory in Config Data Scripts Server Ultima Spawns; do
    if [[ ! -d "$STAGING_DIR/$directory" ]]; then
        log "ERROR: Full server release is missing required directory: $directory."
        exit 1
    fi
done

if [[ ! -s "$STAGING_DIR/servuo.service" ]]; then
    log "ERROR: Full server release does not contain servuo.service."
    exit 1
fi

for file in ServUO.exe Scripts.dll Ultima.dll; do
    if [[ ! -s "$STAGING_DIR/$file" ]]; then
        log "ERROR: Required release file is missing or empty: $file."
        exit 1
    fi
done

shopt -s nullglob
release_binaries=("$STAGING_DIR"/*.dll "$STAGING_DIR"/*.exe)
if [[ ${#release_binaries[@]} -eq 0 ]]; then
    log "ERROR: Release archive contains no assemblies."
    exit 1
fi

if systemctl cat servuo-auto-update.timer >/dev/null 2>&1; then
    log "Disabling the legacy auto-update timer."
    # Use non-interactive sudo or remove if not needed
    sudo -n systemctl disable --now servuo-auto-update.timer || log "Could not disable timer (sudo required)"
fi

if systemctl cat servuo-auto-update.service >/dev/null 2>&1; then
    sudo -n systemctl stop servuo-auto-update.service || log "Could not stop auto-update service (sudo required)"
fi

mkdir -p "$REPO_DIR"
BACKUP_DIR="$REPO_DIR/Backups/deploy/$(date -u +%Y%m%dT%H%M%SZ)-$RELEASE_SHA"
existing_files=("$REPO_DIR"/*.dll "$REPO_DIR"/*.exe)

binary_changed=false
for file in "${release_binaries[@]}"; do
    installed_file="$REPO_DIR/${file##*/}"
    if [[ ! -f "$installed_file" ]] || ! cmp -s "$file" "$installed_file"; then
        binary_changed=true
        break
    fi
done

if [[ "$binary_changed" == false ]]; then
    for file in "${existing_files[@]}"; do
        if [[ ! -f "$STAGING_DIR/${file##*/}" ]]; then
            binary_changed=true
            break
        fi
    done
fi

if [[ "$binary_changed" == false ]]; then
    cp -a "$STAGING_DIR/." "$REPO_DIR/"
    if ! cmp -s "$STAGING_DIR/servuo.service" /etc/systemd/system/servuo.service 2>/dev/null; then
        sudo -n install -m 0644 "$STAGING_DIR/servuo.service" /etc/systemd/system/servuo.service || { log "ERROR: Failed to update systemd unit. Sudo NOPASSWD for 'install' required."; exit 1; }
        sudo -n systemctl daemon-reload
    else
        log "Updated the full server tree; binaries are unchanged, so the ServUO restart was skipped."
    fi
    rm -rf -- "$STAGING_DIR" "$ARCHIVE"
    exit 0
fi

mkdir -p "$BACKUP_DIR"
if [[ ${#existing_files[@]} -gt 0 ]]; then
    cp -a "${existing_files[@]}" "$BACKUP_DIR/"
fi

if [[ -f /etc/systemd/system/servuo.service ]]; then
    cp -a /etc/systemd/system/servuo.service "$BACKUP_DIR/servuo.service"
fi

log "Stopping ServUO."
if systemctl cat servuo >/dev/null 2>&1; then
    sudo -n systemctl stop servuo || { log "ERROR: Failed to stop ServUO. Sudo NOPASSWD for 'systemctl stop' required."; exit 1; }
fi

for file in "${existing_files[@]}"; do
    rm -- "$file"
done
cp -a "$STAGING_DIR/." "$REPO_DIR/"

sudo -n install -m 0644 "$STAGING_DIR/servuo.service" /etc/systemd/system/servuo.service || { log "ERROR: Failed to install systemd unit. Sudo NOPASSWD for 'install' required."; exit 1; }
sudo -n systemctl daemon-reload

if sudo -n systemctl restart servuo && sleep 5 && sudo -n systemctl is-active --quiet servuo; then
    log "Release $RELEASE_SHA deployed successfully."
    rm -rf -- "$STAGING_DIR" "$ARCHIVE"
    exit 0
fi

log "ERROR: New release failed to start; restoring previous binaries."
if [[ ${#existing_files[@]} -eq 0 ]]; then
    log "No previous binaries were present; leaving the first release installed for diagnosis."
    exit 1
fi

deployed_files=("$REPO_DIR"/*.dll "$REPO_DIR"/*.exe)
for file in "${deployed_files[@]}"; do
    rm -- "$file"
done
backup_files=("$BACKUP_DIR"/*.dll "$BACKUP_DIR"/*.exe)
if [[ ${#backup_files[@]} -gt 0 ]]; then
    cp -a "${backup_files[@]}" "$REPO_DIR/"
fi

if [[ -f "$BACKUP_DIR/servuo.service" ]]; then
    sudo -n install -m 0644 "$BACKUP_DIR/servuo.service" /etc/systemd/system/servuo.service
    sudo -n systemctl daemon-reload
fi

if ! sudo -n systemctl restart servuo; then
    log "CRITICAL: Rollback restart failed; inspect systemctl status servuo."
fi

exit 1
