import useApiQuery from 'Helpers/Hooks/useApiQuery';

export interface RecycleBinItem {
  fileName: string;
  fileSize: number;
  rootFolderPath: string;
  recycledAt: string;
  expiresAt: string | null;
}

const useRecycleBin = () => {
  const result = useApiQuery<RecycleBinItem[]>({ path: '/recyclebin' });
  return { ...result, data: result.data ?? [] };
};

export default useRecycleBin;
