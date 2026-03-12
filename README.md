# Streamarr

Streamarr is a media manager for YouTube and Twitch creators. It tracks channels, automatically downloads new videos and streams via yt-dlp, organizes them with configurable naming, and integrates with Plex for a seamless library experience.

Built as a fork of [Sonarr](https://github.com/Sonarr/Sonarr), Streamarr keeps the proven infrastructure (download clients, notifications, scheduling, API) while replacing the TV domain with a streaming content model.

## Status

**Early development** — core functionality works but expect rough edges. See the roadmap below for progress.

## Features

- Add creators and track their YouTube channels and Twitch streams
- Automatic download of new videos and VODs via yt-dlp
- Configurable file organization and renaming (tokens for creator, title, date, etc.)
- Quality profiles and format selection
- Scheduled metadata sync (every 60 minutes)
- Notification support (Discord, Telegram, email, webhooks, and more)
- REST API and web UI
- Docker support with PUID/PGID remapping

## Roadmap

- **Phase 0: Foundation** ✅ — Rename and rebrand from Sonarr
- **Phase 1: MVP** ✅ — Creator/Channel/Content domain models, YouTube metadata, yt-dlp integration, basic UI
- **Phase 2: Twitch** — Twitch metadata via Helix API, live stream recording
- **Phase 3: Plex** — Custom metadata agent, NFO sidecar generation

## Docker

The quickest way to get started:

```bash
curl -o docker-compose.yml https://raw.githubusercontent.com/nardokor/streamarr/main/docker-compose.example.yml
# edit PUID, PGID, TZ, and the downloads path
docker compose up -d
```

Then open [http://localhost:8990](http://localhost:8990).

The image is published to GHCR on every push to `main`:

```
ghcr.io/nardokor/streamarr:latest
```

yt-dlp is installed inside the container and can update itself without a restart via the scheduled task in Settings → General.

## Tech Stack

- .NET 10 (C#)
- React 18 + TypeScript
- SQLite / PostgreSQL (via Dapper)
- yt-dlp

## Development

```bash
# Backend
dotnet build src/Streamarr.sln
dotnet run --project src/Streamarr.Console

# Frontend
yarn install
yarn build
```

The backend serves the frontend from `_output/UI` by default. For frontend hot-reload during development, run `yarn start` and proxy API calls to the backend port (8990).

## Disclaimer

Streamarr is an independent project with no affiliation with or endorsement from the [Servarr](https://github.com/Servarr) organisation or any of its projects (Sonarr, Radarr, Lidarr, etc.).

This project was built with the assistance of [Claude Code](https://claude.ai/claude-code), an AI coding agent by Anthropic.

## License

- [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
- Based on [Sonarr](https://github.com/Sonarr/Sonarr), Copyright 2010-2025 Sonarr contributors
