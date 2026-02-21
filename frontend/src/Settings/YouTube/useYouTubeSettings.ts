import {
  useManageSettings,
  useSaveSettings,
  useSettings,
} from 'Settings/useSettings';
import { useCallback } from 'react';

export interface YouTubeSettingsModel {
  youTubeApiKey: string;
}

const PATH = '/settings/youtube';

export const useYouTubeSettings = () => useSettings<YouTubeSettingsModel>(PATH);

export const useManageYouTubeSettings = () =>
  useManageSettings<YouTubeSettingsModel>(PATH);

export const useSaveYouTubeSettings = () => {
  const { data } = useSettings<YouTubeSettingsModel>(PATH);
  const { save } = useSaveSettings<YouTubeSettingsModel>(PATH);

  const saveSettings = useCallback(
    (changes: Partial<YouTubeSettingsModel>) => {
      const updatedSettings = { ...data, ...changes };
      save(updatedSettings);
    },
    [data, save]
  );

  return saveSettings;
};
