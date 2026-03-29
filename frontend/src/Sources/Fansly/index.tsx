import { SourceDescriptor } from '../types';

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'Fansly',
    channelPlatform: 'fansly',
    implementation: 'Fansly',
    searchPlaceholder: 'Fansly URL or username (e.g. https://fansly.com/creatorname)',
    buildContentUrl: (id) => `https://fansly.com/post/${id}`,
    videosLabel: 'Posts',
    shortsLabel: 'Posts',
    contentTypeLabel: (ct) => {
      switch (ct) {
        case 'video': return 'Video Post';
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
