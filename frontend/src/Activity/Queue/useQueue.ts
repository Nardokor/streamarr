import useApiQuery from 'Helpers/Hooks/useApiQuery';

export type QueueItemState =
  | 'queued'
  | 'waitingForSlot'
  | 'downloading'
  | 'liveWaiting';

export interface QueueItem {
  commandId: number;
  contentId: number;
  contentTitle: string;
  thumbnailUrl: string;
  creatorName: string;
  channelName: string;
  status: 'queued' | 'started';
  message: string;
  queuedAt: string;
  startedAt: string | null;
  state: QueueItemState;
}

export interface QueueSlots {
  configuredMax: number;
  effectiveMax: number;
  availableSlots: number;
  activeDownloadContentIds: number[];
  liveWaitingContentIds: number[];
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

export function useQueueSlots() {
  return useApiQuery<QueueSlots>({
    path: '/queue/slots',
    queryOptions: {
      refetchInterval: 5000,
    },
  });
}
