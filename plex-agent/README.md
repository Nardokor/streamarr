# Streamarr Plex Metadata Agent

Reads NFO sidecars written by Streamarr and surfaces creator/video metadata inside Plex as a TV library.

## Plex mapping

| Streamarr | Plex |
|-----------|------|
| Creator folder | TV Show |
| Year of upload | Season (e.g. Season 2024) |
| Video | Episode (ordered by air date within the year) |

Show metadata (title, description, poster) comes from `tvshow.nfo` in the creator folder. Episode metadata (title, description, thumbnail, air date, runtime) comes from the `.nfo` sidecar alongside each video file.

## Installation

1. Copy `Streamarr.bundle` into your Plex plug-ins directory:

   | Platform | Path |
   |----------|------|
   | Linux | `$PLEX_HOME/Library/Application Support/Plex Media Server/Plug-ins/` |
   | macOS | `~/Library/Application Support/Plex Media Server/Plug-ins/` |
   | Windows | `%LOCALAPPDATA%\Plex Media Server\Plug-ins\` |

2. Restart Plex Media Server.

3. In Plex, create a new **TV Shows** library pointed at your Streamarr root folder.

4. In the library's **Advanced** settings, set the agent to **Streamarr**.

5. Scan the library. Plex will call the agent for each creator folder it finds.

## Requirements

- Plex Media Server with plug-in support (Legacy Plex Pass or self-hosted).
- Streamarr configured to write NFO sidecars (enabled by default).
