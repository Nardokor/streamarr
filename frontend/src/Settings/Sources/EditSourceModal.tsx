import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import { inputTypes } from 'Helpers/Props';
import { useManageYouTubeSettings } from 'Settings/YouTube/useYouTubeSettings';
import { InputChanged } from 'typings/inputs';

interface EditSourceModalProps {
  source: string | null;
  isOpen: boolean;
  onModalClose: () => void;
}

function YouTubeSourceForm({ onModalClose }: { onModalClose: () => void }) {
  const { settings, isSaving, saveSettings, updateSetting } =
    useManageYouTubeSettings();

  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

  const { mutate: runTest, isPending: isTesting } = useApiMutation<
    void,
    { apiKey: string }
  >({
    path: '/settings/youtube/test',
    method: 'POST',
    mutationOptions: {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Connection successful');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(
          err.statusBody?.message ?? err.statusText ?? 'Connection failed'
        );
      },
    },
  });

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error name needs to be keyof YouTubeSettingsModel
      updateSetting(change.name, change.value);
    },
    [updateSetting]
  );

  const handleTest = useCallback(() => {
    runTest({ apiKey: settings.apiKey.value ?? '' });
  }, [runTest, settings.apiKey.value]);

  const handleSave = useCallback(() => {
    saveSettings();
    onModalClose();
  }, [saveSettings, onModalClose]);

  return (
    <>
      <ModalHeader>YouTube</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>API Key</FormLabel>

          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="apiKey"
            helpText="YouTube Data API v3 key from Google Cloud Console."
            onChange={handleInputChange}
            {...settings.apiKey}
          />
        </FormGroup>

        {testResult !== null && (
          <Alert kind={testResult === 'success' ? 'success' : 'danger'}>
            {testMessage}
          </Alert>
        )}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>Cancel</Button>

        <SpinnerButton isSpinning={isTesting} onPress={handleTest}>
          Test
        </SpinnerButton>

        <SpinnerButton isSpinning={isSaving} onPress={handleSave}>
          Save
        </SpinnerButton>
      </ModalFooter>
    </>
  );
}

function EditSourceModal({ source, isOpen, onModalClose }: EditSourceModalProps) {
  return (
    <Modal isOpen={isOpen} size="small" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        {source === 'youtube' ? (
          <YouTubeSourceForm onModalClose={onModalClose} />
        ) : null}
      </ModalContent>
    </Modal>
  );
}

export default EditSourceModal;
