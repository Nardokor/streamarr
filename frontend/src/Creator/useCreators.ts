import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import fetchJson from 'Utilities/Fetch/fetchJson';
import getQueryPath from 'Utilities/Fetch/getQueryPath';
import Channel from 'typings/Channel';
import Content from 'typings/Content';
import Creator, { CreatorLookupChannel, CreatorLookupResult } from 'typings/Creator';

export interface AddCreatorPayload extends Partial<Creator> {
  channels: CreatorLookupChannel[];
}

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

export const useCreator = (id: number) => {
  return useApiQuery<Creator>({
    path: `${CREATORS_PATH}/${id}`,
  });
};

export const useCreatorBySlug = (slug: string) => {
  return useApiQuery<Creator>({
    path: `${CREATORS_PATH}/slug/${slug}`,
    queryOptions: { enabled: slug.length > 0 },
  });
};

export const useCreatorChannels = (creatorId: number) => {
  const result = useApiQuery<Channel[]>({
    path: `/channel/creator/${creatorId}`,
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};

export const useAllChannels = () => {
  const result = useApiQuery<Channel[]>({
    path: '/channel',
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};

export const useCreatorContent = (creatorId: number) => {
  const result = useApiQuery<Content[]>({
    path: `/content/creator/${creatorId}`,
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};

export const useCreatorLookup = (term: string, platform?: string) => {
  const result = useApiQuery<CreatorLookupResult>({
    path: '/creator/lookup',
    queryParams: { term, ...(platform ? { platform } : {}) },
    queryOptions: {
      enabled: term.length > 0,
    },
  });

  return result;
};

export const useAddCreator = () => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<
    number,
    AddCreatorPayload
  >({
    path: CREATORS_PATH,
    method: 'POST',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [CREATORS_PATH] });
      },
    },
  });

  return { addCreator: mutate, isAdding: isPending, addError: error };
};

export const useAddChannel = (creatorId: number) => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<number, Partial<Channel>>({
    path: '/channel',
    method: 'POST',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [`/channel/creator/${creatorId}`] });
      },
    },
  });

  return { addChannel: mutate, isAdding: isPending, addError: error };
};

export const useUpdateChannel = (id: number, creatorId: number) => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<number, Partial<Channel>>({
    path: `/channel/${id}`,
    method: 'PUT',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [`/channel/creator/${creatorId}`] });
      },
    },
  });

  return { updateChannel: mutate, isUpdating: isPending, updateError: error };
};

export const useDeleteChannel = (id: number, creatorId: number) => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<object, void>({
    path: `/channel/${id}`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [`/channel/creator/${creatorId}`] });
      },
    },
  });

  return { deleteChannel: mutate, isDeleting: isPending, deleteError: error };
};

export const useUpdateCreator = (id: number) => {
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useApiMutation<number, Partial<Creator>>({
    path: `${CREATORS_PATH}/${id}`,
    method: 'PUT',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [CREATORS_PATH] });
        queryClient.invalidateQueries({ queryKey: [`${CREATORS_PATH}/${id}`] });
      },
    },
  });

  return { updateCreator: mutate, isUpdating: isPending, updateError: error };
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

export interface CreatorStats {
  creatorId: number;
  downloadedCount: number;
  wantedCount: number;
  isLiveNow: boolean;
  liveCategory: string;
  hasMissing: boolean;
  hasActiveMembership: boolean;
}

export const useCreatorStats = () => {
  const result = useApiQuery<CreatorStats[]>({
    path: `${CREATORS_PATH}/stats`,
  });

  return {
    ...result,
    data: result.data ?? [],
  };
};

export const useReorderChannels = (creatorId: number) => {
  const queryClient = useQueryClient();

  return useCallback(
    async (channels: Channel[]) => {
      await Promise.all(
        channels.map((ch) =>
          fetchJson<number, Partial<Channel>>({
            path: getQueryPath(`/channel/${ch.id}`),
            method: 'PUT',
            body: ch,
            headers: {
              'X-Api-Key': window.Streamarr.apiKey,
              'X-Streamarr-Client': 'Streamarr',
            },
          })
        )
      );
      queryClient.invalidateQueries({ queryKey: [`/channel/creator/${creatorId}`] });
    },
    [creatorId, queryClient]
  );
};
