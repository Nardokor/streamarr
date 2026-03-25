import useApiQuery from 'Helpers/Hooks/useApiQuery';

interface YtDlpStatus {
  version: string | null;
}

const useYtDlpStatus = () => {
  return useApiQuery<YtDlpStatus>({ path: '/ytdlp/status' });
};

export default useYtDlpStatus;
