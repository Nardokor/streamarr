# Streamarr

Streamarr is a media manager for YouTube and Twitch creators. It tracks channels, automatically downloads new videos and streams via yt-dlp, organizes them with configurable naming, and integrates with Plex for a seamless library experience.

Built as a fork of [Sonarr](https://github.com/Sonarr/Sonarr), Streamarr keeps the proven infrastructure (download clients, notifications, scheduling, API) while replacing the TV domain with a streaming content model.

## Status

**Early development** - not yet usable. See the roadmap below for progress.

## Planned Features

- Track YouTube channels and Twitch streamers
- Automatic download of new videos and VODs via yt-dlp
- Configurable file organization and renaming
- Plex integration with custom metadata (NFO sidecars)
- Quality profiles and automatic upgrades
- Notification support (Discord, Telegram, email, webhooks, etc.)
- REST API and web UI
- Support for Linux, macOS, and Windows

## Roadmap

- **Phase 0: Foundation** - Rename and rebrand from Sonarr (in progress)
- **Phase 1: MVP** - Channel/Content domain models, YouTube metadata, yt-dlp integration, basic UI
- **Phase 2: Twitch** - Twitch metadata via Helix API, live stream recording
- **Phase 3: Plex** - Custom metadata agent, NFO sidecar generation

## Tech Stack

- .NET 10 (C#)
- React 18 + TypeScript (frontend)
- SQLite / PostgreSQL (via Dapper)
- yt-dlp (download engine)

## Development

```bash
# Build
dotnet build src/Streamarr.sln

# Run
dotnet run --project src/Streamarr.Host
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup details.

## License

- [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
- Based on [Sonarr](https://github.com/Sonarr/Sonarr), Copyright 2010-2025 Sonarr contributors
