import { useQueryClient } from '@tanstack/react-query';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Creator, { CreatorLookupResult } from 'typings/Creator';

const CREATORS_PATH = '/creator';

const useCreators = () => {
  const result = useApiQuery<Creator[]>({
    path: CREATORS_PATH,
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};

export default useCreators;

export const useCreatorLookup = (term: string) => {
  const result = useApiQuery<CreatorLookupResult>({
    path: '/creator/lookup',
    queryParams: { term },
    queryOptions: {
      enabled: term.length > 0,
    },
  });

  return result;
};

export const useAddCreator = () => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<number, Partial<Creator>>(
    {
      path: CREATORS_PATH,
      method: 'POST',
      mutationOptions: {
        onSuccess: () => {
          queryClient.invalidateQueries({ queryKey: [CREATORS_PATH] });
        },
      },
    }
  );

  return { addCreator: mutate, isAdding: isPending, addError: error };
};

export const useDeleteCreator = (id: number) => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<object, void>({
    path: `${CREATORS_PATH}/${id}`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [CREATORS_PATH] });
      },
    },
  });

  return {
    deleteCreator: mutate,
    isDeleting: isPending,
    deleteError: error,
  };
};
