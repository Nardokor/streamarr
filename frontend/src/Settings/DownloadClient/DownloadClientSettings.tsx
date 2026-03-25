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
import SpinnerButton from 'Components/Link/SpinnerButton';
import CommandNames from 'Commands/CommandNames';
import { useCommandExecuting, useExecuteCommand } from 'Commands/useCommands';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import SettingsToolbar from 'Settings/SettingsToolbar';
import { InputChanged } from 'typings/inputs';
import { useManageDownloadClientSettings } from './useDownloadClientSettings';
import useYtDlpStatus from './useYtDlpStatus';

function DownloadClientSettings() {
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
  } = useManageDownloadClientSettings();

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error name needs to be keyof DownloadClientSettingsModel
      updateSetting(change.name, change.value);
    },
    [updateSetting]
  );

  const { data: ytDlpStatus } = useYtDlpStatus();
  const executeCommand = useExecuteCommand();
  const isUpdating = useCommandExecuting(CommandNames.UpdateYtDlp);

  const handleSavePress = useCallback(() => {
    saveSettings();
  }, [saveSettings]);

  const handleUpdateYtDlp = useCallback(() => {
    executeCommand({ name: CommandNames.UpdateYtDlp });
  }, [executeCommand]);

  return (
    <PageContent title="Download Client Settings">
      <SettingsToolbar
        hasPendingChanges={hasPendingChanges}
        isSaving={isSaving}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching && !isPopulated ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>Failed to load download client settings.</Alert>
        ) : null}

        {hasSettings && isPopulated && !error ? (
          <Form
            id="downloadClientSettings"
            validationErrors={validationErrors}
            validationWarnings={validationWarnings}
          >
            <FieldSet legend="yt-dlp">
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Version</FormLabel>

                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <span>{ytDlpStatus?.version ?? 'Unknown'}</span>

                  <SpinnerButton
                    isSpinning={isUpdating}
                    onPress={handleUpdateYtDlp}
                  >
                    Update yt-dlp
                  </SpinnerButton>
                </div>
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Binary Path</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="ytDlpBinaryPath"
                  helpText="Path to the yt-dlp binary. Defaults to 'yt-dlp' (must be on PATH)."
                  onChange={handleInputChange}
                  {...settings.ytDlpBinaryPath}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Deno Binary Path</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="ytDlpDenoBinaryPath"
                  helpText="Path to the deno binary used by yt-dlp for JavaScript challenge solving (required for members-only content). Defaults to 'deno' (must be on PATH)."
                  onChange={handleInputChange}
                  {...settings.ytDlpDenoBinaryPath}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Temp Download Folder</FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="ytDlpTempDownloadFolder"
                  helpText="Temporary folder for in-progress downloads. Leave empty to download directly to the destination."
                  includeFiles={false}
                  onChange={handleInputChange}
                  {...settings.ytDlpTempDownloadFolder}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Cookie File</FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="ytDlpCookieFilePath"
                  helpText="Path to a Netscape-format cookies file for authenticated downloads."
                  includeFiles={true}
                  onChange={handleInputChange}
                  {...settings.ytDlpCookieFilePath}
                />
              </FormGroup>

              <FormGroup size={sizes.LARGE}>
                <FormLabel>Preferred Format</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="ytDlpPreferredFormat"
                  helpText="yt-dlp format selector string. E.g. 'bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best'"
                  onChange={handleInputChange}
                  {...settings.ytDlpPreferredFormat}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Max Concurrent Downloads</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="ytDlpMaxConcurrentDownloads"
                  min={1}
                  max={10}
                  helpText="Maximum number of simultaneous yt-dlp download processes."
                  onChange={handleInputChange}
                  {...settings.ytDlpMaxConcurrentDownloads}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Embed Metadata</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="ytDlpEmbedMetadata"
                  helpText="Embed title, description, and other metadata into the downloaded file."
                  onChange={handleInputChange}
                  {...settings.ytDlpEmbedMetadata}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Embed Thumbnail</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="ytDlpEmbedThumbnail"
                  helpText="Embed the video thumbnail into the downloaded file."
                  onChange={handleInputChange}
                  {...settings.ytDlpEmbedThumbnail}
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default DownloadClientSettings;
