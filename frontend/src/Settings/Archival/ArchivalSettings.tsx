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
import { useManageArchivalSettings } from './useArchivalSettings';

function ArchivalSettings() {
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
  } = useManageArchivalSettings();

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error name needs to be keyof ArchivalSettingsModel
      updateSetting(change.name, change.value);
    },
    [updateSetting]
  );

  const handleSavePress = useCallback(() => {
    saveSettings();
  }, [saveSettings]);

  return (
    <PageContent title="Archival Settings">
      <SettingsToolbar
        hasPendingChanges={hasPendingChanges}
        isSaving={isSaving}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching && isPopulated ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>Failed to load archival settings.</Alert>
        ) : null}

        {hasSettings && isPopulated && !error ? (
          <Form
            id="archivalSettings"
            validationErrors={validationErrors}
            validationWarnings={validationWarnings}
          >
            <FieldSet legend="Priority Keywords">
              <FormGroup>
                <FormLabel>Global Priority Keywords</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="globalPriorityKeywords"
                  helpText="Comma or space separated keywords. Content with matching titles is downloaded regardless of type or title filters."
                  onChange={handleInputChange}
                  {...settings.globalPriorityKeywords}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend="Retention">
              <FormGroup>
                <FormLabel>Default Retention Days</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="defaultRetentionDays"
                  unit="days"
                  helpText="Number of days to keep downloaded content before expiring. Set to 0 to keep forever."
                  onChange={handleInputChange}
                  {...settings.defaultRetentionDays}
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default ArchivalSettings;
