import { useQueryClient } from '@tanstack/react-query';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';

const ORPHANED_FILES_PATH = '/system/orphanedfiles';

export interface OrphanedFile {
  path: string;
  fileName: string;
  creatorId: number;
  creatorTitle: string;
  size: number;
  lastWriteUtc: string;
  stale: boolean;
}

export function useOrphanedFiles() {
  const result = useApiQuery<OrphanedFile[]>({
    path: ORPHANED_FILES_PATH,
  });

  return {
    ...result,
    data: result.data ?? [],
  };
}

export function useDeleteOrphanedFile(path: string) {
  const queryClient = useQueryClient();

  const { mutate, isPending } = useApiMutation<void, void>({
    path: ORPHANED_FILES_PATH,
    method: 'DELETE',
    queryParams: { path },
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [ORPHANED_FILES_PATH] });
      },
    },
  });

  return { deleteFile: mutate, isDeleting: isPending };
}
