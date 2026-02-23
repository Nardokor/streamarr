import React, { useCallback, useEffect } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { EnhancedSelectInputValue } from 'Components/Form/Select/EnhancedSelectInput';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import {
  NamingSettingsModel,
  useManageNamingSettings,
} from './useNamingSettings';

const colonReplacementOptions: EnhancedSelectInputValue<number>[] = [
  { key: 4, value: 'Smart (replaces with a dash near spaces)' },
  { key: 0, value: 'Delete' },
  { key: 1, value: 'Dash' },
  { key: 2, value: 'Space Dash' },
  { key: 3, value: 'Space Dash Space' },
];

interface NamingProps {
  setChildSave: (saveCallback: () => void) => void;
  onChildStateChange: (state: {
    isSaving: boolean;
    hasPendingChanges: boolean;
  }) => void;
}

function Naming({ setChildSave, onChildStateChange }: NamingProps) {
  const {
    settings,
    updateSetting,
    isFetching,
    error,
    hasSettings,
    hasPendingChanges,
    isSaving,
    saveSettings,
  } = useManageNamingSettings();

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      updateSetting(
        change.name as keyof NamingSettingsModel,
        change.value as NamingSettingsModel[keyof NamingSettingsModel]
      );
    },
    [updateSetting]
  );

  useEffect(() => {
    onChildStateChange({ hasPendingChanges, isSaving });
  }, [hasPendingChanges, isSaving, onChildStateChange]);

  useEffect(() => {
    setChildSave(saveSettings);
  }, [setChildSave, saveSettings]);

  return (
    <FieldSet legend="Content Naming">
      {isFetching ? <LoadingIndicator /> : null}

      {!isFetching && error ? (
        <Alert kind={kinds.DANGER}>
          Failed to load naming settings.
        </Alert>
      ) : null}

      {hasSettings && !isFetching && !error ? (
        <Form>
          <FormGroup size={sizes.MEDIUM}>
            <FormLabel>Rename Downloaded Content</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="renameContent"
              helpText="Rename content files after download"
              onChange={handleInputChange}
              {...settings.renameContent}
            />
          </FormGroup>

          <FormGroup size={sizes.LARGE}>
            <FormLabel>Content File Format</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="contentFileFormat"
              helpTexts={[
                'Template for downloaded file names.',
                'Tokens: {Content Title}, {Content Id}, {Creator Title}, {Channel Title}, {Published Date}, {Year}, {Month}, {Day}, {Content Type}, {Quality Title}',
              ]}
              onChange={handleInputChange}
              {...settings.contentFileFormat}
            />
          </FormGroup>

          <FormGroup size={sizes.LARGE}>
            <FormLabel>Creator Folder Format</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="creatorFolderFormat"
              helpText="Template for the creator root folder. Tokens: {Creator Title}, {Creator CleanTitle}"
              onChange={handleInputChange}
              {...settings.creatorFolderFormat}
            />
          </FormGroup>

          <FormGroup size={sizes.MEDIUM}>
            <FormLabel>Colon Replacement</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="colonReplacementFormat"
              values={colonReplacementOptions}
              helpText="How to handle colons in file and folder names"
              onChange={handleInputChange}
              {...settings.colonReplacementFormat}
            />
          </FormGroup>

          <FormGroup size={sizes.MEDIUM}>
            <FormLabel>Replace Illegal Characters</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="replaceIllegalCharacters"
              helpText="Replace characters not allowed in file names"
              onChange={handleInputChange}
              {...settings.replaceIllegalCharacters}
            />
          </FormGroup>
        </Form>
      ) : null}
    </FieldSet>
  );
}

export default Naming;
