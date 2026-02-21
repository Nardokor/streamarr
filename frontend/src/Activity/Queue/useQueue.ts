import useApiQuery from 'Helpers/Hooks/useApiQuery';

export interface QueueItem {
  commandId: number;
  contentId: number;
  contentTitle: string;
  thumbnailUrl: string;
  creatorName: string;
  channelName: string;
  status: 'queued' | 'started';
  message: string;
}

export function useQueue() {
  return useApiQuery<QueueItem[]>({
    path: '/queue',
    queryOptions: {
      refetchInterval: 5000,
      placeholderData: [],
    },
  });
}
