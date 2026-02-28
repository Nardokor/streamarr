import useApiMutation from 'Helpers/Hooks/useApiMutation';

export interface ImportableFolder {
  folderName: string;
  path: string;
}

export interface ImportFoldersRequest {
  rootPath: string;
}

export interface ImportLibraryRequest {
  rootPath: string;
  folderNames: string[];
}

export interface ImportLibraryResult {
  creatorsCreated: number;
  creatorsMatched: number;
  channelsCreated: number;
  contentLinked: number;
  contentAlreadyLinked: number;
  filesNotMatched: number;
}

export const useGetImportableFolders = () => {
  const { mutate, isPending, data, error } = useApiMutation<ImportableFolder[], ImportFoldersRequest>({
    path: '/import/folders',
    method: 'POST',
  });

  return { scanFolders: mutate, isScanning: isPending, folders: data ?? [], scanError: error };
};

export const useImportLibrary = () => {
  const { mutate, isPending, data, error } = useApiMutation<ImportLibraryResult, ImportLibraryRequest>({
    path: '/import',
    method: 'POST',
  });

  return { importLibrary: mutate, isImporting: isPending, result: data, importError: error };
};
