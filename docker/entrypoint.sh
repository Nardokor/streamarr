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

# Pull latest yt-dlp nightly before handing off to the app.
# Running as root here so we have write access to /usr/local/bin/yt-dlp.
echo "Updating yt-dlp to latest nightly..."
yt-dlp --update-to nightly || echo "yt-dlp update failed (no internet?), continuing with existing version"

exec gosu streamarr /app/Streamarr --nobrowser --data=/config "$@"
