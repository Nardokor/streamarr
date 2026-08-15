import useApiMutation from 'Helpers/Hooks/useApiMutation';

export type UrlImportStatus =
  | 'started'
  | 'alreadyDownloaded'
  | 'needsTarget'
  | 'error';

export interface UrlImportResult {
  status: UrlImportStatus;
  message: string;
  contentId: number | null;
  creatorId: number | null;
  creatorTitle: string;
  channelId: number | null;
  resolvedTitle: string;
  resolvedChannelTitle: string;
  thumbnailUrl: string;
}

export interface UrlImportPayload {
  url: string;
  channelId?: number;
}

const useImportUrl = () => {
  const { mutate, isPending, error, data, reset } = useApiMutation<
    UrlImportResult,
    UrlImportPayload
  >({
    path: '/import/url',
    method: 'POST',
  });

  return {
    importUrl: mutate,
    isImporting: isPending,
    importError: error,
    result: data,
    reset,
  };
};

export default useImportUrl;
