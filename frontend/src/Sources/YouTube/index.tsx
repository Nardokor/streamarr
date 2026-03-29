import { SourceDescriptor } from '../types';
import type { ContentType } from 'typings/Content';

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'YouTube',
    channelPlatform: 'youTube',
    implementation: 'YouTube',
    searchPlaceholder: 'YouTube @handle, channel URL, or name',
    badgeVariant: undefined,
    buildContentUrl: (id) => `https://www.youtube.com/watch?v=${id}`,
    videosLabel: 'Videos',
    shortsLabel: 'Shorts',
    contentTypeLabel: (ct: ContentType) => {
      switch (ct) {
        case 'video': return 'Video';
        case 'short': return 'Short';
        case 'vod': return 'VoD';
        case 'live': return 'Live';
        case 'upcoming': return 'Upcoming';
        default: return '';
      }
    },
    showMembershipButton: true,
  },
};

export default descriptor;
