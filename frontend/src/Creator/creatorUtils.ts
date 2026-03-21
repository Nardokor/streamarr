import Content, { ContentType } from 'typings/Content';

export function getContentTypeLabel(contentType: ContentType, platform?: string): string {
  const isTwitch = platform === 'twitch';
  switch (contentType) {
    case 'video': return isTwitch ? 'Highlight' : 'Video';
    case 'short': return isTwitch ? 'Clip' : 'Short';
    case 'vod': return 'VoD';
    case 'live': return 'Live';
    case 'upcoming': return 'Upcoming';
    default: return '';
  }
}

export function getTypeLabels(platform?: string): string[] {
  const isTwitch = platform === 'twitch';
  return [
    isTwitch ? 'Highlight' : 'Video',
    isTwitch ? 'Clip' : 'Short',
    'VoD',
    'Live',
    'Upcoming',
  ];
}

export function getShortsLabel(platform?: string): string {
  return platform === 'twitch' ? 'Clips' : 'Shorts';
}

export function getVideosLabel(platform?: string): string {
  return platform === 'twitch' ? 'Highlights' : 'Videos';
}

export function formatDuration(duration: string | null): string {
  if (!duration) {
    return '—';
  }
  // .NET TimeSpan serialises as "HH:mm:ss"
  const parts = duration.split(':');
  if (parts.length === 3) {
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    const s = parseInt(parts[2], 10);
    if (h > 0) {
      return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }
    return `${m}:${String(s).padStart(2, '0')}`;
  }
  return duration;
}

export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) {
    return '—';
  }
  return new Date(dateStr).toLocaleDateString();
}

export interface StatusLabel {
  text: string;
  kind: 'downloaded' | 'downloading' | 'queued' | 'recording' | 'missing' | 'unmonitored' | 'notAired' | 'expired' | 'modified' | 'unwanted' | 'processing' | 'available' | 'unavailable';
}

export function getStatusLabel(content: Content): StatusLabel {
  // Members content the current cookies cannot unlock
  if (content.isMembers && !content.isAccessible) {
    return { text: 'Unavailable', kind: 'unavailable' };
  }

  // Upcoming — scheduled but not yet started
  if (content.contentType === 'upcoming') {
    return { text: 'Not Aired', kind: 'notAired' };
  }

  if (content.status === 'recording') {
    return { text: 'Recording', kind: 'recording' };
  }

  if (content.status === 'queued') {
    return { text: 'Queued', kind: 'queued' };
  }

  if (content.status === 'downloading') {
    return { text: 'Downloading', kind: 'downloading' };
  }

  if (content.status === 'processing') {
    return { text: 'Processing', kind: 'processing' };
  }

  if (content.status === 'expired') {
    return { text: 'Expired', kind: 'expired' };
  }

  if (content.status === 'modified') {
    return { text: 'Modified', kind: 'modified' };
  }

  if (content.status === 'downloaded' || content.contentFileId > 0) {
    return { text: 'Downloaded', kind: 'downloaded' };
  }

  if (content.status === 'unwanted') {
    return { text: 'Unwanted', kind: 'unwanted' };
  }

  if (content.status === 'available') {
    return { text: 'Available', kind: 'available' };
  }

  if (content.monitored) {
    return { text: 'Missing', kind: 'missing' };
  }

  return { text: 'Unmonitored', kind: 'unmonitored' };
}

export function buildPlatformUrl(
  platform: string,
  platformContentId: string
): string | null {
  switch (platform) {
    case 'youTube':
      return `https://www.youtube.com/watch?v=${platformContentId}`;
    case 'twitch':
      return platformContentId.startsWith('https://')
        ? platformContentId
        : `https://www.twitch.tv/videos/${platformContentId}`;
    case 'fourthwall':
      return `https://www.youtube.com/watch?v=${platformContentId}`;
    case 'fansly':
      return `https://fansly.com/post/${platformContentId}`;
    case 'party':
      return `https://party.gg/${platformContentId}`;
    case 'patreon':
      return `https://www.patreon.com/posts/${platformContentId}`;
    case 'twitter':
      return `https://x.com/i/status/${platformContentId}`;
    default:
      return null;
  }
}

export function getNextLiveDate(content: Content[]): Date | null {
  const now = new Date();
  const upcoming = content
    .filter((c) => c.contentType === 'upcoming' && c.airDateUtc && new Date(c.airDateUtc) > now)
    .map((c) => new Date(c.airDateUtc!))
    .sort((a, b) => a.getTime() - b.getTime());
  return upcoming.length > 0 ? upcoming[0] : null;
}
