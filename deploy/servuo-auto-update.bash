#!/usr/bin/env bash
set -Eeuo pipefail

REPO_DIR="/root/ServUO"
BRANCH="main"
REMOTE="origin"
LOCK_FILE="/run/lock/servuo-auto-update.lock"

log() {
    printf '[servuo-auto-update] %s\n' "$*"
}

exec 9>"$LOCK_FILE"
if ! flock -n 9; then
    log "Another update check is already running; skipping."
    exit 0
fi

if [[ ! -d "$REPO_DIR/.git" ]]; then
    log "ERROR: Git repository not found at $REPO_DIR."
    exit 1
fi

current_branch="$(git -C "$REPO_DIR" branch --show-current)"
if [[ "$current_branch" != "$BRANCH" ]]; then
    log "ERROR: Expected branch '$BRANCH', found '${current_branch:-detached HEAD}'."
    exit 1
fi

if [[ -n "$(git -C "$REPO_DIR" status --porcelain)" ]]; then
    log "ERROR: Working tree is not clean; refusing to update."
    exit 1
fi

log "Fetching $REMOTE/$BRANCH."
git -C "$REPO_DIR" fetch --prune "$REMOTE" "$BRANCH"

local_head="$(git -C "$REPO_DIR" rev-parse HEAD)"
remote_head="$(git -C "$REPO_DIR" rev-parse FETCH_HEAD)"

if [[ "$local_head" == "$remote_head" ]]; then
    log "Already up to date."
    exit 0
fi

if git -C "$REPO_DIR" merge-base --is-ancestor "$remote_head" "$local_head"; then
    log "Local main contains commits not present on $REMOTE/$BRANCH; no upstream updates to apply."
    exit 0
fi

if git -C "$REPO_DIR" merge-base --is-ancestor "$local_head" "$remote_head"; then
    log "Fast-forwarding to $remote_head."
    git -C "$REPO_DIR" merge --ff-only "$remote_head"
else
    read -r local_only remote_only <<<"$(git -C "$REPO_DIR" rev-list --left-right --count "$local_head...$remote_head")"
    log "Histories diverged: $local_only local commit(s), $remote_only upstream commit(s)."
    log "Merging $REMOTE/$BRANCH into local main to preserve both histories."

    if ! git -C "$REPO_DIR" merge --no-edit "$remote_head"; then
        log "ERROR: Merge could not be completed. Resolve conflicts in $REPO_DIR and finish the merge; servuo was not restarted."
        exit 1
    fi
fi

log "Restarting servuo."
systemctl stop servuo && systemctl start servuo
log "Update and restart completed."
