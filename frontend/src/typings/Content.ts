import ModelBase from 'App/ModelBase';

export type ContentType = 'unknown' | 'video' | 'short' | 'vod' | 'live' | 'upcoming';
export type ContentStatus = 'unknown' | 'missing' | 'downloading' | 'downloaded' | 'deleted' | 'recording' | 'expired' | 'modified' | 'unwanted' | 'processing' | 'available';

interface Content extends ModelBase {
  channelId: number;
  contentFileId: number;
  platformContentId: string;
  contentType: ContentType;
  title: string;
  description: string;
  thumbnailUrl: string;
  duration: string | null;
  airDateUtc: string | null;
  dateAdded: string;
  monitored: boolean;
  status: ContentStatus;
  // Populated only on single-item fetch
  fileRelativePath?: string | null;
  fileSize?: number | null;
}

export default Content;
