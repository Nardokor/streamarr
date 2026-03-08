"""
Streamarr Plex Metadata Agent
------------------------------
Reads NFO sidecars written by Streamarr and maps them onto Plex TV-show
metadata.

Folder layout (produced by Streamarr):
    <Creator>/
        tvshow.nfo          ← show-level metadata
        video-title.mkv
        video-title.nfo     ← episode-level metadata
        ...

Plex mapping:
    Creator folder  →  TV Show
    Year of upload  →  Season  (e.g. 2024 → Season 2024)
    Video           →  Episode (ordered by air date within the year)
"""

import os
import datetime

try:
    from xml.etree import cElementTree as ET
except ImportError:
    from xml.etree import ElementTree as ET

BUNDLE_ID = 'com.streamarr.agents.streamarr'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _read_nfo(path):
    """Parse an NFO XML file and return the root element, or None on error."""
    try:
        return ET.parse(path).getroot()
    except Exception as exc:
        Log.Error('[Streamarr] Failed to parse NFO %s: %s', path, exc)
        return None


def _tvshow_nfo(folder):
    path = os.path.join(folder, 'tvshow.nfo')
    return path if os.path.isfile(path) else None


def _episode_nfo(video_path):
    base = os.path.splitext(video_path)[0]
    path = base + '.nfo'
    return path if os.path.isfile(path) else None


def _show_dir_from_media(media):
    """Walk the Plex media tree to find the first real file path."""
    try:
        for s in media.seasons:
            for e in media.seasons[s].episodes:
                for item in media.seasons[s].episodes[e].items:
                    for part in item.parts:
                        if part.file:
                            # Videos are directly inside the creator folder.
                            return os.path.dirname(part.file)
    except Exception:
        pass
    return None


def _load_poster(metadata, url):
    try:
        data = HTTP.Request(url, timeout=15).content
        metadata.posters[url] = Proxy.Preview(data, sort_order=1)
    except Exception as exc:
        Log.Warn('[Streamarr] Could not fetch poster %s: %s', url, exc)


def _load_thumb(ep_meta, url):
    try:
        data = HTTP.Request(url, timeout=15).content
        ep_meta.thumbs[url] = Proxy.Preview(data, sort_order=1)
    except Exception as exc:
        Log.Warn('[Streamarr] Could not fetch thumb %s: %s', url, exc)


# ---------------------------------------------------------------------------
# Collect episode NFOs from the show folder and build a season/episode map.
# Season  = year of <aired> (integer).
# Episode = 1-based position within that year, ordered by air date then title.
# ---------------------------------------------------------------------------

def _build_episode_map(show_dir):
    """
    Returns a dict:
        { video_path: (season_num, episode_num, nfo_root) }
    where season_num is the year (int) and episode_num is 1-based within the
    year, sorted by (air_date, title).
    """
    entries = []  # (air_date, title, video_path, nfo_root)

    for name in os.listdir(show_dir):
        if name == 'tvshow.nfo':
            continue
        if not name.endswith('.nfo'):
            continue

        nfo_path = os.path.join(show_dir, name)
        root = _read_nfo(nfo_path)
        if root is None:
            continue

        # Find the matching video file (any extension alongside the nfo)
        base = os.path.splitext(nfo_path)[0]
        video_path = None
        for ext in ('.mkv', '.mp4', '.webm', '.mov', '.avi', '.flv', '.m4v'):
            candidate = base + ext
            if os.path.isfile(candidate):
                video_path = candidate
                break

        if not video_path:
            continue

        title = (root.findtext('title') or '').strip()

        aired_str = (root.findtext('aired') or '').strip()
        try:
            air_date = datetime.datetime.strptime(aired_str, '%Y-%m-%d').date()
        except ValueError:
            # No valid air date — sort to end of time
            air_date = datetime.date(9999, 12, 31)

        entries.append((air_date, title, video_path, root))

    # Sort by (air_date, title)
    entries.sort(key=lambda x: (x[0], x[1]))

    # Assign season (year) and episode number
    year_counters = {}
    result = {}

    for air_date, title, video_path, root in entries:
        year = air_date.year if air_date.year != 9999 else 0
        year_counters[year] = year_counters.get(year, 0) + 1
        ep_num = year_counters[year]
        result[video_path] = (year, ep_num, root)

    return result


# ---------------------------------------------------------------------------
# Agent
# ---------------------------------------------------------------------------

class StreamarrAgent(Agent.TV_Shows):
    name = 'Streamarr'
    languages = [Locale.Language.English]
    primary_provider = True
    accepts_from = ['com.plexapp.agents.localmedia']

    def search(self, results, media, lang):
        show_dir = _show_dir_from_media(media)

        title = media.show  # fallback: folder name
        if show_dir:
            nfo_path = _tvshow_nfo(show_dir)
            if nfo_path:
                root = _read_nfo(nfo_path)
                if root is not None:
                    t = (root.findtext('title') or '').strip()
                    if t:
                        title = t

        results.Append(MetadataSearchResult(
            id=show_dir or media.show,
            name=title,
            score=100,
            lang=lang,
        ))

    def update(self, metadata, media, lang):
        show_dir = _show_dir_from_media(media)
        if not show_dir:
            Log.Error('[Streamarr] Could not determine show directory')
            return

        # ------------------------------------------------------------------
        # Show-level metadata from tvshow.nfo
        # ------------------------------------------------------------------
        nfo_path = _tvshow_nfo(show_dir)
        if nfo_path:
            root = _read_nfo(nfo_path)
            if root is not None:
                title = (root.findtext('title') or '').strip()
                if title:
                    metadata.title = title

                plot = (root.findtext('plot') or '').strip()
                if plot:
                    metadata.summary = plot

                thumb = root.find('thumb')
                if thumb is not None and (thumb.text or '').strip():
                    _load_poster(metadata, thumb.text.strip())

        # ------------------------------------------------------------------
        # Build the full season/episode map from NFOs on disk so we know the
        # correct season and episode numbers for every file.
        # ------------------------------------------------------------------
        ep_map = _build_episode_map(show_dir)  # video_path → (season, ep, root)

        # ------------------------------------------------------------------
        # Populate episode metadata.  Plex gives us the files it found; we
        # look up each one in ep_map and write into the right season/episode
        # slot.
        # ------------------------------------------------------------------
        for s_key in media.seasons:
            for e_key in media.seasons[s_key].episodes:
                ep = media.seasons[s_key].episodes[e_key]
                try:
                    video_path = ep.items[0].parts[0].file
                except (IndexError, AttributeError):
                    continue

                entry = ep_map.get(video_path)
                if not entry:
                    Log.Debug('[Streamarr] No NFO mapping for %s', video_path)
                    continue

                season_num, ep_num, root = entry
                ep_meta = metadata.seasons[season_num].episodes[ep_num]

                title = (root.findtext('title') or '').strip()
                if title:
                    ep_meta.title = title

                plot = (root.findtext('plot') or '').strip()
                if plot:
                    ep_meta.summary = plot

                aired = (root.findtext('aired') or '').strip()
                if aired:
                    try:
                        d = datetime.datetime.strptime(aired, '%Y-%m-%d').date()
                        ep_meta.originally_available_at = d
                        ep_meta.year = d.year
                    except ValueError:
                        pass

                runtime = (root.findtext('runtime') or '').strip()
                if runtime:
                    try:
                        ep_meta.duration = int(runtime) * 60 * 1000  # ms
                    except ValueError:
                        pass

                thumb_url = (root.findtext('thumb') or '').strip()
                if thumb_url:
                    _load_thumb(ep_meta, thumb_url)

                Log.Debug(
                    '[Streamarr] Mapped %s → S%dE%d "%s"',
                    os.path.basename(video_path), season_num, ep_num, title
                )
