import { SourceDescriptor } from '../types';
import type { ContentType } from 'typings/Content';

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'Fourthwall',
    channelPlatform: 'fourthwall',
    implementation: 'Fourthwall',
    searchPlaceholder: 'Full site URL (e.g. https://namijifreesia.party/)',
    badgeVariant: 'fourthwall',
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
    showMembershipButton: false,
  },
};

export default descriptor;
