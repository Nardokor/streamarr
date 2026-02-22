import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Content from 'typings/Content';

export const useWantedMissing = () => {
  const result = useApiQuery<Content[]>({
    path: '/wanted/missing',
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};
