import { useManageSettings, useSettings } from 'Settings/useSettings';

const PATH = '/settings/naming';

export interface NamingSettingsModel {
  renameContent: boolean;
  replaceIllegalCharacters: boolean;
  colonReplacementFormat: number;
  contentFileFormat: string;
  creatorFolderFormat: string;
}

export const useNamingSettings = () => {
  return useSettings<NamingSettingsModel>(PATH);
};

export const useManageNamingSettings = () => {
  return useManageSettings<NamingSettingsModel>(PATH);
};
