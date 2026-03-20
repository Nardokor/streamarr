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
  category: string;
  sortOrder: number;
  monitored: boolean;
  status: ChannelStatusType;
  lastInfoSync: string | null;
  membershipStatus: 'unknown' | 'active' | 'none';
  lastMembershipCheck: string | null;

  // Wanted — content types
  downloadVideos: boolean;
  downloadShorts: boolean;
  downloadVods: boolean;
  downloadLive: boolean;
  downloadMembers: boolean;

  // Wanted — word filters
  watchedWords: string;
  ignoredWords: string;
  watchedDefeatsIgnored: boolean;

  // Download mode
  autoDownload: boolean;

  // Retention
  retentionDays: number | null;
  keepVideos: boolean;
  keepShorts: boolean;
  keepVods: boolean;
  keepMembers: boolean;
  retentionKeepWords: string;
}

export default Channel;
