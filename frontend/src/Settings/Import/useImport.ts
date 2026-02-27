import useApiMutation from 'Helpers/Hooks/useApiMutation';

export interface ImportLibraryRequest {
  rootPath: string;
  qualityProfileId?: number | null;
}

export interface ImportLibraryResult {
  creatorsCreated: number;
  creatorsMatched: number;
  channelsCreated: number;
  contentLinked: number;
  contentAlreadyLinked: number;
  filesNotMatched: number;
}

export const useImportLibrary = () => {
  const { mutate, isPending, data, error } = useApiMutation<ImportLibraryResult, ImportLibraryRequest>({
    path: '/import',
    method: 'POST',
  });

  return { importLibrary: mutate, isImporting: isPending, result: data, importError: error };
};
