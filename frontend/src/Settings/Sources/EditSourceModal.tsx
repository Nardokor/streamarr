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
    { youTubeApiKey: string }
  >({
    path: '/settings/youtube/test',
    method: 'POST',
  });

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error name needs to be keyof YouTubeSettingsModel
      updateSetting(change.name, change.value);
    },
    [updateSetting]
  );

  const handleTest = useCallback(() => {
    runTest(
      { youTubeApiKey: settings.youTubeApiKey.value ?? '' },
      {
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
      }
    );
  }, [runTest, settings.youTubeApiKey.value]);

  const handleSave = useCallback(() => {
    const apiKey = settings.youTubeApiKey.value ?? '';

    if (apiKey) {
      runTest(
        { youTubeApiKey: apiKey },
        {
          onSuccess: () => {
            setTestResult('success');
            setTestMessage('Connection successful');
            saveSettings();
            onModalClose();
          },
          onError: (err) => {
            setTestResult('failure');
            setTestMessage(
              err.statusBody?.message ?? err.statusText ?? 'Connection failed'
            );
          },
        }
      );
    } else {
      saveSettings();
      onModalClose();
    }
  }, [settings.youTubeApiKey.value, runTest, saveSettings, onModalClose]);

  return (
    <>
      <ModalHeader>YouTube</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>API Key</FormLabel>

          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="youTubeApiKey"
            helpText="YouTube Data API v3 key from Google Cloud Console."
            onChange={handleInputChange}
            {...settings.youTubeApiKey}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Full Refresh Interval (hours)</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="youTubeFullRefreshIntervalHours"
            helpText="How often to scan for new videos (min 1, max 168)"
            min={1}
            max={168}
            onChange={handleInputChange}
            {...settings.youTubeFullRefreshIntervalHours}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Live Check Interval (minutes)</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="youTubeLiveCheckIntervalMinutes"
            helpText="How often to check livestream status (min 5, max 1440)"
            min={5}
            max={1440}
            onChange={handleInputChange}
            {...settings.youTubeLiveCheckIntervalMinutes}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Download Videos</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultDownloadVideos"
            helpText="Include regular videos by default for new channels"
            onChange={handleInputChange}
            {...settings.youTubeDefaultDownloadVideos}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Download Shorts</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultDownloadShorts"
            helpText="Include shorts by default for new channels"
            onChange={handleInputChange}
            {...settings.youTubeDefaultDownloadShorts}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Download VoDs</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultDownloadVods"
            helpText="Include past livestreams by default for new channels"
            onChange={handleInputChange}
            {...settings.youTubeDefaultDownloadVods}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Download Live</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultDownloadLive"
            helpText="Record active livestreams by default for new channels"
            onChange={handleInputChange}
            {...settings.youTubeDefaultDownloadLive}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Watched Words</FormLabel>
          <FormInputGroup
            type={inputTypes.TEXT}
            name="youTubeDefaultWatchedWords"
            helpText="word1, word2 … — only matching content is wanted (blank = all)"
            onChange={handleInputChange}
            {...settings.youTubeDefaultWatchedWords}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Ignored Words</FormLabel>
          <FormInputGroup
            type={inputTypes.TEXT}
            name="youTubeDefaultIgnoredWords"
            helpText="word1, word2 … — matching content is unwanted (blank = none)"
            onChange={handleInputChange}
            {...settings.youTubeDefaultIgnoredWords}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Watched Defeats Ignored</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultWatchedDefeatsIgnored"
            helpText="Watched words take priority over ignored words"
            onChange={handleInputChange}
            {...settings.youTubeDefaultWatchedDefeatsIgnored}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Default: Auto Download</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="youTubeDefaultAutoDownload"
            helpText="Automatically queue missing content for download"
            onChange={handleInputChange}
            {...settings.youTubeDefaultAutoDownload}
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
    <Modal isOpen={isOpen} size="medium" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        {source === 'youtube' ? (
          <YouTubeSourceForm onModalClose={onModalClose} />
        ) : null}
      </ModalContent>
    </Modal>
  );
}

export default EditSourceModal;
