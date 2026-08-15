import { useManageSettings } from 'Settings/useSettings';

export interface DownloadClientSettingsModel {
  ytDlpBinaryPath: string;
  ytDlpTempDownloadFolder: string;
  ytDlpEmbedMetadata: boolean;
  ytDlpEmbedThumbnail: boolean;
  ytDlpPreferredFormat: string;
  ytDlpMaxConcurrentDownloads: number;
  ytDlpDenoBinaryPath: string;
}

export const useManageDownloadClientSettings = () =>
  useManageSettings<DownloadClientSettingsModel>('/settings/downloadclient');
