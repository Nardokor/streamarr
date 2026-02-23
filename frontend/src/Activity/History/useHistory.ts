import useApiQuery from 'Helpers/Hooks/useApiQuery';

export interface HistoryRecord {
  id: number;
  contentId: number;
  channelId: number;
  creatorId: number;
  title: string;
  eventType: string;
  data: string;
  date: string;
}

export const useHistory = () => {
  const result = useApiQuery<HistoryRecord[]>({
    path: '/history',
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};
