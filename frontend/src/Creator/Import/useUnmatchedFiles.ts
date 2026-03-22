import { useQueryClient } from '@tanstack/react-query';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';

export interface UnmatchedFile {
  id: number;
  creatorId: number;
  filePath: string;
  fileName: string;
  fileSize: number;
  dateFound: string;
  reason: number;
}

export const unmatchedFileReasonLabel: Record<number, string> = {
  0: 'No YouTube ID in filename',
  1: 'No metadata source configured',
  2: 'Metadata not found',
  3: 'No channel ID in metadata',
};

const UNMATCHED_PATH = '/unmatchedfile';

export const useUnmatchedFilesByCreator = (creatorId: number) => {
  const path = `${UNMATCHED_PATH}/creator/${creatorId}`;
  const result = useApiQuery<UnmatchedFile[]>({
    path,
    queryOptions: { enabled: creatorId > 0 },
  });
  return { ...result, data: result.data ?? [] };
};

export const useAssignUnmatchedFile = (id: number, creatorId: number) => {
  const queryClient = useQueryClient();
  const { mutate, isPending, error } = useApiMutation<unknown, { channelId: number }>({
    path: `${UNMATCHED_PATH}/${id}/assign`,
    method: 'POST',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: [`${UNMATCHED_PATH}/creator/${creatorId}`],
        });
      },
    },
  });
  return { assign: mutate, isAssigning: isPending, assignError: error };
};

export const useDismissUnmatchedFile = (id: number, creatorId: number) => {
  const queryClient = useQueryClient();
  const { mutate, isPending, error } = useApiMutation<unknown, void>({
    path: `${UNMATCHED_PATH}/${id}`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: [`${UNMATCHED_PATH}/creator/${creatorId}`],
        });
      },
    },
  });
  return { dismiss: mutate, isDismissing: isPending, dismissError: error };
};

export const useDeleteUnmatchedFile = (id: number, creatorId: number) => {
  const queryClient = useQueryClient();
  const { mutate, isPending, error } = useApiMutation<unknown, void>({
    path: `${UNMATCHED_PATH}/${id}/file`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: [`${UNMATCHED_PATH}/creator/${creatorId}`],
        });
      },
    },
  });
  return { deleteFile: mutate, isDeletingFile: isPending, deleteFileError: error };
};
