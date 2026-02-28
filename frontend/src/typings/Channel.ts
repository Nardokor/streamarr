import ModelBase from 'App/ModelBase';

export type ChannelStatusType = 0 | 1 | 2;

interface Channel extends ModelBase {
  creatorId: number;
  platform: string;
  platformId: string;
  platformUrl: string;
  title: string;
  description: string;
  thumbnailUrl: string;
  monitored: boolean;
  status: ChannelStatusType;
  lastInfoSync: string | null;

  // Wanted — content types
  downloadVideos: boolean;
  downloadShorts: boolean;
  downloadVods: boolean;
  downloadLive: boolean;

  // Wanted — word filters
  watchedWords: string;
  ignoredWords: string;
  watchedDefeatsIgnored: boolean;

  // Download mode
  autoDownload: boolean;

  // Retention
  retentionDays: number | null;
  retentionVideos: boolean;
  retentionShorts: boolean;
  retentionVods: boolean;
  retentionLive: boolean;
  retentionExceptionWords: string;
}

export default Channel;
