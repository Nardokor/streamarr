import { useManageSettings, useSettings } from 'Settings/useSettings';

export interface MediaManagementSettingsModel {
  recycleBin: string;
  recycleBinCleanupDays: number;
  deleteEmptyFolders: boolean;
  setPermissionsLinux: boolean;
  chmodFolder: string;
  chownGroup: string;
  skipFreeSpaceCheckWhenImporting: boolean;
  minimumFreeSpaceWhenImporting: number;
  copyUsingHardlinks: boolean;
}

export interface NamingSettingsModel {
  renameContent: boolean;
  replaceIllegalCharacters: boolean;
  colonReplacementFormat: number;
  contentFileFormat: string;
  creatorFolderFormat: string;
}

const MEDIA_MANAGEMENT_PATH = '/settings/mediamanagement';
const NAMING_PATH = '/settings/naming';

export const useMediaManagementSettings = () => {
  return useSettings<MediaManagementSettingsModel>(MEDIA_MANAGEMENT_PATH);
};

export const useManageMediaManagementSettings = () => {
  return useManageSettings<MediaManagementSettingsModel>(MEDIA_MANAGEMENT_PATH);
};

export const useManageNamingSettings = () => {
  return useManageSettings<NamingSettingsModel>(NAMING_PATH);
};
