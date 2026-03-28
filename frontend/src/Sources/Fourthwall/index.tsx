import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes, kinds } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import {
  MetadataSourceResource,
  applyFieldChanges,
  getFieldValue,
  useCreateMetadataSource,
  useDeleteMetadataSource,
  useMetadataSources,
  useTestMetadataSource,
  useUpdateMetadataSource,
} from 'Settings/Sources/useMetadataSources';
import { SourceDescriptor, SourceFormProps } from '../types';
import BaseSettingsFields from '../BaseSettingsFields';

function FourthwallSourceForm({ source, onModalClose }: SourceFormProps) {
  const isNew = !source.id;

  const { data: allSources } = useMetadataSources();
  const youtubeApiConfigured = (allSources ?? []).some(
    (s) =>
      s.implementation === 'YouTube' &&
      s.enable &&
      getFieldValue<string>(s.fields, 'apiKey', '').trim() !== ''
  );

  const [enabled, setEnabled] = useState(source.enable ?? true);
  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(null);
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(source.id ?? 0);
  const isSaving = isCreating || isUpdating;
  const { mutate: deleteSource, isPending: isDeleting } = useDeleteMetadataSource(source.id ?? 0);
  const { mutate: runTest, isPending: isTesting } = useTestMetadataSource();

  const getVal = useCallback(
    <T,>(name: string, fallback: T): T => {
      if (name in pending) return pending[name] as T;
      return getFieldValue<T>(source.fields, name, fallback);
    },
    [pending, source.fields]
  );

  const handleInputChange = useCallback((change: InputChanged) => {
    setPending((prev) => ({ ...prev, [change.name]: change.value }));
  }, []);

  const buildUpdatedSource = useCallback(
    (): MetadataSourceResource => ({
      ...source,
      name: source.name || 'Fourthwall',
      enable: enabled,
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, enabled, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Cookies file found');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Test failed');
      },
    });
  }, [runTest, buildUpdatedSource]);

  const handleSave = useCallback(() => {
    const updated = buildUpdatedSource();
    const save = isNew ? create : update;
    save(updated, {
      onSuccess: () => onModalClose(),
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Save failed');
      },
    });
  }, [buildUpdatedSource, isNew, create, update, onModalClose]);

  return (
    <>
      <ModalHeader>Fourthwall</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>Enable</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="enable"
            helpText="Enable this source for content syncing and channel searches."
            value={enabled}
            errors={[]}
            warnings={[]}
            onChange={(change: InputChanged) => setEnabled(change.value as boolean)}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Cookies File</FormLabel>
          <FormInputGroup
            type={inputTypes.TEXT}
            name="cookiesFilePath"
            helpText="Path to a Netscape-format cookies.txt file exported from your browser while logged in to Fourthwall."
            value={getVal('cookiesFilePath', '')}
            errors={[]}
            warnings={[]}
            onChange={handleInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Use YouTube API for Live Detection</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="useYouTubeApi"
            helpText={
              youtubeApiConfigured
                ? 'Use the YouTube Data API to detect upcoming and live streams. Streams posted to Fourthwall before going live will be detected as Upcoming and recorded automatically when they start.'
                : 'Requires a YouTube source with an API key configured in Settings \u203a Sources.'
            }
            value={youtubeApiConfigured ? getVal('useYouTubeApi', true) : false}
            isDisabled={!youtubeApiConfigured}
            errors={[]}
            warnings={[]}
            onChange={youtubeApiConfigured ? handleInputChange : () => undefined}
          />
        </FormGroup>

        <BaseSettingsFields
          getVal={getVal}
          onChange={handleInputChange}
          showVideos={true}
          showShorts={false}
          showVods={false}
          showLive={true}
          showFilters={false}
        />

        {testResult !== null && (
          <Alert kind={testResult === 'success' ? 'success' : 'danger'}>
            {testMessage}
          </Alert>
        )}
      </ModalBody>

      <ModalFooter>
        {!isNew && (
          <div style={{ marginRight: 'auto' }}>
            <SpinnerButton
              kind={kinds.DANGER}
              isSpinning={isDeleting}
              onPress={() => deleteSource(undefined, { onSuccess: () => onModalClose() })}
            >
              Delete
            </SpinnerButton>
          </div>
        )}

        <Button onPress={onModalClose}>Cancel</Button>

        <SpinnerButton isSpinning={isTesting} onPress={handleTest}>
          Test
        </SpinnerButton>

        <SpinnerButton isSpinning={isSaving} onPress={handleSave}>
          {isNew ? 'Add' : 'Save'}
        </SpinnerButton>
      </ModalFooter>
    </>
  );
}

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'Fourthwall',
    channelPlatform: 'fourthwall',
    implementation: 'Fourthwall',
    searchPlaceholder: 'Full site URL (e.g. https://namijifreesia.party/)',
    badgeVariant: 'fourthwall',
    buildContentUrl: (id) => `https://www.youtube.com/watch?v=${id}`,
    videosLabel: 'Videos',
    shortsLabel: 'Shorts',
    contentTypeLabel: (ct) => {
      switch (ct) {
        case 'video': return 'Video';
        case 'short': return 'Short';
        case 'vod': return 'VoD';
        case 'live': return 'Live';
        case 'upcoming': return 'Upcoming';
        default: return '';
      }
    },
    showMembershipButton: false,
  },
  SettingsForm: FourthwallSourceForm,
};

export default descriptor;
