import ModelBase from 'App/ModelBase';

export type ContentType = 0 | 1 | 2 | 3;
export type ContentStatus = 0 | 1 | 2 | 3;

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
