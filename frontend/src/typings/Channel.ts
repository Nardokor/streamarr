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
}

export default Channel;
