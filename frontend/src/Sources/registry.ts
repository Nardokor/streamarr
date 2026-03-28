import { PlatformConfig, SourceDescriptor } from './types';
import YouTube from './YouTube';
import Twitch from './Twitch';
import Fourthwall from './Fourthwall';

// Keyed by MetadataSourceResource.implementation ('YouTube', 'Twitch', 'Fourthwall')
export const SOURCE_REGISTRY: Record<string, SourceDescriptor> = {
  YouTube,
  Twitch,
  Fourthwall,
};

// Keyed by Channel.platform camelCase ('youTube', 'twitch', 'fourthwall')
export const PLATFORM_REGISTRY: Record<string, PlatformConfig> = Object.fromEntries(
  Object.values(SOURCE_REGISTRY).map((d) => [d.platformConfig.channelPlatform, d.platformConfig])
);
