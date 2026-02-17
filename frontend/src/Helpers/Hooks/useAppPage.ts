import { useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { useTranslations } from 'App/useTranslations';
import useCommands from 'Commands/useCommands';
import useCustomFilters from 'Filters/useCustomFilters';
import { useQualityProfiles } from 'Settings/Profiles/Quality/useQualityProfiles';
import { useUiSettings } from 'Settings/UI/useUiSettings';
import { fetchCustomFilters } from 'Store/Actions/customFilterActions';
import {
  fetchLanguages,
} from 'Store/Actions/settingsActions';
import useSystemStatus from 'System/Status/useSystemStatus';
import useTags from 'Tags/useTags';
import { ApiError } from 'Utilities/Fetch/fetchJson';

const createErrorsSelector = ({
  customFiltersError,
  systemStatusError,
  tagsError,
  translationsError,
  uiSettingsError,
  qualityProfilesError,
}: {
  customFiltersError: ApiError | null;
  systemStatusError: ApiError | null;
  tagsError: ApiError | null;
  translationsError: ApiError | null;
  uiSettingsError: ApiError | null;
  qualityProfilesError: ApiError | null;
}) =>
  createSelector(
    (state: AppState) => state.settings.languages.error,
    (languagesError) => {
      const hasError = !!(
        customFiltersError ||
        uiSettingsError ||
        qualityProfilesError ||
        languagesError ||
        systemStatusError ||
        tagsError ||
        translationsError
      );

      return {
        hasError,
        errors: {
          customFiltersError,
          tagsError,
          uiSettingsError,
          qualityProfilesError,
          languagesError,
          systemStatusError,
          translationsError,
        },
      };
    }
  );

const useAppPage = () => {
  const dispatch = useDispatch();

  useCommands();

  const { isFetched: isCustomFiltersFetched, error: customFiltersError } =
    useCustomFilters();

  const { isFetched: isSystemStatusFetched, error: systemStatusError } =
    useSystemStatus();

  const { isFetched: isTagsFetched, error: tagsError } = useTags();

  const { isFetched: isTranslationsFetched, error: translationsError } =
    useTranslations();

  const { isFetched: isUiSettingsFetched, error: uiSettingsError } =
    useUiSettings();

  const { isFetched: isQualityProfilesFetched, error: qualityProfilesError } =
    useQualityProfiles();

  const isAppStatePopulated = useSelector(
    (state: AppState) =>
      state.settings.languages.isPopulated
  );

  const isPopulated =
    isAppStatePopulated &&
    isCustomFiltersFetched &&
    isSystemStatusFetched &&
    isTagsFetched &&
    isTranslationsFetched &&
    isUiSettingsFetched &&
    isQualityProfilesFetched;

  const { hasError, errors } = useSelector(
    createErrorsSelector({
      customFiltersError,
      systemStatusError,
      tagsError,
      translationsError,
      uiSettingsError,
      qualityProfilesError,
    })
  );

  const isLocalStorageSupported = useMemo(() => {
    const key = 'streamarrTest';

    try {
      localStorage.setItem(key, key);
      localStorage.removeItem(key);

      return true;
    } catch {
      return false;
    }
  }, []);

  useEffect(() => {
    dispatch(fetchCustomFilters());
    dispatch(fetchLanguages());
  }, [dispatch]);

  return useMemo(() => {
    return { errors, hasError, isLocalStorageSupported, isPopulated };
  }, [errors, hasError, isLocalStorageSupported, isPopulated]);
};

export default useAppPage;
