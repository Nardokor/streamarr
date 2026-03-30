import { SourceDescriptor } from '../types';
import type { ContentType } from 'typings/Content';

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'Patreon',
    channelPlatform: 'patreon',
    implementation: 'Patreon',
    searchPlaceholder: 'Patreon URL or username (e.g. https://www.patreon.com/creatorname)',
    buildContentUrl: (id) => `https://www.patreon.com/posts/${id}`,
    videosLabel: 'Posts',
    shortsLabel: 'Posts',
    contentTypeLabel: (ct: ContentType) => {
      switch (ct) {
        case 'video': return 'Video';
        case 'short': return 'Post';
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
