import Content, { ContentType } from 'typings/Content';

export function getContentTypeLabel(contentType: ContentType): string {
  switch (contentType) {
    case 1: return 'Video';
    case 2: return 'Short';
    case 3: return 'Live';
    default: return '';
  }
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
  kind: 'downloaded' | 'missing' | 'unmonitored';
}

export function getStatusLabel(content: Content): StatusLabel {
  if (content.contentFileId > 0) {
    return { text: 'Downloaded', kind: 'downloaded' };
  }
  if (content.monitored) {
    return { text: 'Missing', kind: 'missing' };
  }
  return { text: 'Unmonitored', kind: 'unmonitored' };
}
