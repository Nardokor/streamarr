import type { ComponentType } from 'react';
import type { ContentType } from 'typings/Content';
import type { MetadataSourceResource } from 'Settings/Sources/useMetadataSources';

export interface SourceFormProps {
  source: MetadataSourceResource;
  onModalClose: () => void;
}

export interface PlatformConfig {
  label: string;
  channelPlatform: string;
  implementation: string;
  searchPlaceholder: string;
  badgeVariant?: 'twitch' | 'fourthwall';
  buildContentUrl: (platformContentId: string) => string | null;
  videosLabel: string;
  shortsLabel: string;
  contentTypeLabel: (contentType: ContentType) => string;
  showMembershipButton?: boolean;
}

export interface SourceDescriptor {
  platformConfig: PlatformConfig;
  SettingsForm?: ComponentType<SourceFormProps>;
}
