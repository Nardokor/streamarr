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

exec gosu streamarr /app/Streamarr --nobrowser --data=/config "$@"
