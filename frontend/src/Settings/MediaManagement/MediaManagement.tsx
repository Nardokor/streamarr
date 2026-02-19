import React, { useCallback } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { EnhancedSelectInputValue } from 'Components/Form/Select/EnhancedSelectInput';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import RootFolders from 'RootFolder/RootFolders';
import { useShowAdvancedSettings } from 'Settings/advancedSettingsStore';
import SettingsToolbar from 'Settings/SettingsToolbar';
import { useIsWindows } from 'System/Status/useSystemStatus';
import { InputChanged } from 'typings/inputs';
import AddRootFolder from './RootFolder/AddRootFolder';
import {
  MediaManagementSettingsModel,
  NamingSettingsModel,
  useManageMediaManagementSettings,
  useManageNamingSettings,
} from './useMediaManagementSettings';

const colonReplacementOptions: EnhancedSelectInputValue<number>[] = [
  { key: 4, value: 'Smart (replaces with a dash near spaces)' },
  { key: 0, value: 'Delete' },
  { key: 1, value: 'Dash' },
  { key: 2, value: 'Space Dash' },
  { key: 3, value: 'Space Dash Space' },
];

function MediaManagement() {
  const showAdvancedSettings = useShowAdvancedSettings();
  const isWindows = useIsWindows();

  const {
    isFetching: isMediaFetching,
    isFetched: isMediaFetched,
    isSaving: isMediaSaving,
    error: mediaError,
    settings: mediaSettings,
    hasSettings: hasMediaSettings,
    hasPendingChanges: hasMediaPendingChanges,
    validationErrors: mediaValidationErrors,
    validationWarnings: mediaValidationWarnings,
    saveSettings: saveMediaSettings,
    updateSetting: updateMediaSetting,
  } = useManageMediaManagementSettings();

  const {
    isFetching: isNamingFetching,
    isFetched: isNamingFetched,
    isSaving: isNamingSaving,
    error: namingError,
    settings: namingSettings,
    hasSettings: hasNamingSettings,
    hasPendingChanges: hasNamingPendingChanges,
    validationErrors: namingValidationErrors,
    validationWarnings: namingValidationWarnings,
    saveSettings: saveNamingSettings,
    updateSetting: updateNamingSetting,
  } = useManageNamingSettings();

  const isSaving = isMediaSaving || isNamingSaving;
  const hasPendingChanges = hasMediaPendingChanges || hasNamingPendingChanges;
  const isFetching = isMediaFetching || isNamingFetching;

  const handleSavePress = useCallback(() => {
    saveMediaSettings();
    saveNamingSettings();
  }, [saveMediaSettings, saveNamingSettings]);

  const handleMediaInputChange = useCallback(
    (change: InputChanged) => {
      updateMediaSetting(
        change.name as keyof MediaManagementSettingsModel,
        change.value as MediaManagementSettingsModel[keyof MediaManagementSettingsModel]
      );
    },
    [updateMediaSetting]
  );

  const handleNamingInputChange = useCallback(
    (change: InputChanged) => {
      updateNamingSetting(
        change.name as keyof NamingSettingsModel,
        change.value as NamingSettingsModel[keyof NamingSettingsModel]
      );
    },
    [updateNamingSetting]
  );

  return (
    <PageContent title="Media Management Settings">
      <SettingsToolbar
        isSaving={isSaving}
        hasPendingChanges={hasPendingChanges}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && (mediaError || namingError) ? (
          <Alert kind={kinds.DANGER}>
            Failed to load settings. Check that Streamarr is running correctly.
          </Alert>
        ) : null}

        {/* Naming */}
        {isNamingFetched && hasNamingSettings && !namingError ? (
          <Form
            id="namingSettings"
            validationErrors={namingValidationErrors}
            validationWarnings={namingValidationWarnings}
          >
            <FieldSet legend="Content Naming">
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Rename Downloaded Content</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="renameContent"
                  helpText="Rename content files after download using the format below"
                  onChange={handleNamingInputChange}
                  {...namingSettings.renameContent}
                />
              </FormGroup>

              <FormGroup size={sizes.LARGE}>
                <FormLabel>Content File Format</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="contentFileFormat"
                  helpTexts={[
                    'Template for downloaded file names.',
                    'Tokens: {Content Title}, {Content Id}, {Creator Title}, {Channel Title}, {Published Date}, {Year}, {Month}, {Day}, {Content Type}, {Quality Title}, {Quality Full}',
                  ]}
                  onChange={handleNamingInputChange}
                  {...namingSettings.contentFileFormat}
                />
              </FormGroup>

              <FormGroup size={sizes.LARGE}>
                <FormLabel>Creator Folder Format</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="creatorFolderFormat"
                  helpText="Template for the creator root folder name. Tokens: {Creator Title}, {Creator CleanTitle}"
                  onChange={handleNamingInputChange}
                  {...namingSettings.creatorFolderFormat}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>Colon Replacement Format</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="colonReplacementFormat"
                  helpText="How to handle colons in file and folder names"
                  values={colonReplacementOptions}
                  onChange={handleNamingInputChange}
                  {...namingSettings.colonReplacementFormat}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>Replace Illegal Characters</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="replaceIllegalCharacters"
                  helpText="Replace characters not allowed in file names"
                  onChange={handleNamingInputChange}
                  {...namingSettings.replaceIllegalCharacters}
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}

        {/* File Management */}
        {isMediaFetched && hasMediaSettings && !mediaError ? (
          <Form
            id="mediaManagementSettings"
            validationErrors={mediaValidationErrors}
            validationWarnings={mediaValidationWarnings}
          >
            <FieldSet legend="File Management">
              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>Delete Empty Folders</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="deleteEmptyFolders"
                  helpText="Remove empty creator folders after content is deleted"
                  onChange={handleMediaInputChange}
                  {...mediaSettings.deleteEmptyFolders}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>Use Hardlinks Instead of Copy</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="copyUsingHardlinks"
                  helpText="Use hardlinks when possible to avoid duplicating disk space"
                  onChange={handleMediaInputChange}
                  {...mediaSettings.copyUsingHardlinks}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>Skip Free Space Check</FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="skipFreeSpaceCheckWhenImporting"
                  helpText="Skip the free space check before downloading"
                  onChange={handleMediaInputChange}
                  {...mediaSettings.skipFreeSpaceCheckWhenImporting}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
                size={sizes.MEDIUM}
              >
                <FormLabel>Minimum Free Space (MB)</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  unit="MB"
                  name="minimumFreeSpaceWhenImporting"
                  helpText="Prevent downloading if disk free space drops below this threshold"
                  onChange={handleMediaInputChange}
                  {...mediaSettings.minimumFreeSpaceWhenImporting}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
              >
                <FormLabel>Recycle Bin</FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="recycleBin"
                  helpText="Deleted files will be moved here instead of permanently deleted"
                  includeFiles={false}
                  onChange={handleMediaInputChange}
                  {...mediaSettings.recycleBin}
                />
              </FormGroup>

              <FormGroup
                advancedSettings={showAdvancedSettings}
                isAdvanced={true}
              >
                <FormLabel>Recycle Bin Cleanup (days)</FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="recycleBinCleanupDays"
                  helpText="Files older than this will be permanently deleted from the recycle bin. Set to 0 to disable."
                  min={0}
                  onChange={handleMediaInputChange}
                  {...mediaSettings.recycleBinCleanupDays}
                />
              </FormGroup>
            </FieldSet>

            {showAdvancedSettings && !isWindows ? (
              <FieldSet legend="Permissions">
                <FormGroup
                  advancedSettings={showAdvancedSettings}
                  isAdvanced={true}
                  size={sizes.MEDIUM}
                >
                  <FormLabel>Set Permissions</FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="setPermissionsLinux"
                    helpText="Set file permissions on downloaded files"
                    onChange={handleMediaInputChange}
                    {...mediaSettings.setPermissionsLinux}
                  />
                </FormGroup>

                <FormGroup
                  advancedSettings={showAdvancedSettings}
                  isAdvanced={true}
                >
                  <FormLabel>chmod Folder</FormLabel>

                  <FormInputGroup
                    type={inputTypes.UMASK}
                    name="chmodFolder"
                    helpText="Permissions to apply to folders (e.g. 755)"
                    onChange={handleMediaInputChange}
                    {...mediaSettings.chmodFolder}
                  />
                </FormGroup>

                <FormGroup
                  advancedSettings={showAdvancedSettings}
                  isAdvanced={true}
                >
                  <FormLabel>chown Group</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="chownGroup"
                    helpText="Group to assign to downloaded files"
                    onChange={handleMediaInputChange}
                    {...mediaSettings.chownGroup}
                  />
                </FormGroup>
              </FieldSet>
            ) : null}
          </Form>
        ) : null}

        <FieldSet legend="Root Folders">
          <RootFolders />
          <AddRootFolder />
        </FieldSet>
      </PageContentBody>
    </PageContent>
  );
}

export default MediaManagement;
