import React, { useCallback } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds } from 'Helpers/Props';
import SettingsToolbar from 'Settings/SettingsToolbar';
import { InputChanged } from 'typings/inputs';
import { useManageYouTubeSettings } from './useYouTubeSettings';

function YouTubeSettings() {
  const {
    isFetching,
    isFetched: isPopulated,
    error,
    hasPendingChanges,
    hasSettings,
    settings,
    isSaving,
    validationErrors,
    validationWarnings,
    saveSettings,
    updateSetting,
  } = useManageYouTubeSettings();

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error name needs to be keyof YouTubeSettingsModel
      updateSetting(change.name, change.value);
    },
    [updateSetting]
  );

  const handleSavePress = useCallback(() => {
    saveSettings();
  }, [saveSettings]);

  return (
    <PageContent title="YouTube Settings">
      <SettingsToolbar
        hasPendingChanges={hasPendingChanges}
        isSaving={isSaving}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching && isPopulated ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>Failed to load YouTube settings.</Alert>
        ) : null}

        {hasSettings && isPopulated && !error ? (
          <Form
            id="youtubeSettings"
            validationErrors={validationErrors}
            validationWarnings={validationWarnings}
          >
            <FieldSet legend="YouTube Data API">
              <FormGroup>
                <FormLabel>API Key</FormLabel>
                <FormInputGroup
                  type={inputTypes.PASSWORD}
                  name="apiKey"
                  helpText="YouTube Data API v3 key from Google Cloud Console. Required for content syncing."
                  onChange={handleInputChange}
                  {...settings.apiKey}
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default YouTubeSettings;
