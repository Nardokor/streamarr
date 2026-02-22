import ModelBase from 'App/ModelBase';

export type ContentType = 'unknown' | 'video' | 'short' | 'livestream';
export type ContentStatus = 'unknown' | 'missing' | 'downloading' | 'downloaded' | 'deleted' | 'live';

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
}

export default Content;
