import { PlatformConfig, SourceDescriptor } from './types';
import YouTube from './YouTube';
import Twitch from './Twitch';
import Fourthwall from './Fourthwall';
import Patreon from './Patreon';
import Fansly from './Fansly';

// Keyed by MetadataSourceResource.implementation ('YouTube', 'Twitch', 'Fourthwall', 'Patreon', 'Fansly')
export const SOURCE_REGISTRY: Record<string, SourceDescriptor> = {
  YouTube,
  Twitch,
  Fourthwall,
  Patreon,
  Fansly,
};

// Keyed by Channel.platform camelCase ('youTube', 'twitch', 'fourthwall')
export const PLATFORM_REGISTRY: Record<string, PlatformConfig> = Object.fromEntries(
  Object.values(SOURCE_REGISTRY).map((d) => [d.platformConfig.channelPlatform, d.platformConfig])
);
