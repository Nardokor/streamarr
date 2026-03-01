import ModelBase from 'App/ModelBase';

export type CreatorStatusType = 0 | 1 | 2;

interface Creator extends ModelBase {
  title: string;
  titleSlug: string;
  description: string;
  thumbnailUrl: string;
  path: string;
  rootFolderPath: string;
  qualityProfileId: number;
  tags: number[];
  monitored: boolean;
  status: CreatorStatusType;
  added: string;
  lastInfoSync: string | null;
}

export interface CreatorLookupResult {
  name: string;
  description: string;
  thumbnailUrl: string;
  channels: CreatorLookupChannel[];
  existingCreatorId?: number;
}

export interface CreatorLookupChannel {
  platform: string;
  platformId: string;
  platformUrl: string;
  title: string;
  description: string;
  thumbnailUrl: string;
}

export default Creator;
