import { useManageSettings } from 'Settings/useSettings';

export interface ArchivalSettingsModel {
  globalPriorityKeywords: string;
  defaultRetentionDays: number;
}

const PATH = '/settings/archival';

export const useManageArchivalSettings = () =>
  useManageSettings<ArchivalSettingsModel>(PATH);
