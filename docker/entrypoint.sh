#!/bin/bash
set -e

PUID=${PUID:-1000}
PGID=${PGID:-1000}

echo "
-------------------------------------
        _
  _____| |_ _ _ ___ ___ _____ _____ ___ ___
 |_-_-_| _| '_/ -_) _' |     / _   / -_) '_|
 |_____|_| |_| \___\__,_|_|_|_\__,_\___|_|

-------------------------------------
User UID:    ${PUID}
User GID:    ${PGID}
-------------------------------------
"

# Apply PUID/PGID
groupmod -o -g "$PGID" streamarr
usermod -o -u "$PUID" streamarr

# Ensure config volume is writable by the process user
mkdir -p /config
chown -R streamarr:streamarr /config

# Re-chown yt-dlp after PUID/PGID remapping so self-update works
chown -R streamarr:streamarr /opt/yt-dlp

# Pull latest yt-dlp nightly before handing off to the app.
# /opt/yt-dlp is owned by streamarr so this also works at runtime
# via the daily UpdateYtDlp scheduled task (no restart needed).
echo "Updating yt-dlp to latest nightly..."
gosu streamarr yt-dlp --update-to nightly || echo "yt-dlp update failed (no internet?), continuing with existing version"

exec gosu streamarr /app/Streamarr --nobrowser --data=/config "$@"
