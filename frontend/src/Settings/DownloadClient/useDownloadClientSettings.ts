import { useManageSettings } from 'Settings/useSettings';

export interface DownloadClientSettingsModel {
  ytDlpBinaryPath: string;
  ytDlpTempDownloadFolder: string;
  ytDlpCookieFilePath: string;
  ytDlpEmbedMetadata: boolean;
  ytDlpEmbedThumbnail: boolean;
  ytDlpPreferredFormat: string;
  ytDlpMaxConcurrentDownloads: number;
}

export const useManageDownloadClientSettings = () =>
  useManageSettings<DownloadClientSettingsModel>('/settings/downloadclient');
