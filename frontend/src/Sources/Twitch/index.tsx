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
  useTestMetadataSource,
  useUpdateMetadataSource,
} from 'Settings/Sources/useMetadataSources';
import { SourceDescriptor, SourceFormProps } from '../types';
import BaseSettingsFields from '../BaseSettingsFields';

function TwitchSourceForm({ source, onModalClose }: SourceFormProps) {
  const isNew = !source.id;

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
      enable: enabled,
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, enabled, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Connection successful');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Connection failed');
      },
    });
  }, [runTest, buildUpdatedSource]);

  const handleSave = useCallback(() => {
    const updated = buildUpdatedSource();
    const clientId = getFieldValue<string>(updated.fields, 'clientId', '');
    const clientSecret = getFieldValue<string>(updated.fields, 'clientSecret', '');
    const save = isNew ? create : update;

    const doSave = () => {
      save(updated, {
        onSuccess: () => onModalClose(),
        onError: (err) => {
          setTestResult('failure');
          setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Save failed');
        },
      });
    };

    if (clientId && clientSecret && updated.enable) {
      runTest(updated, {
        onSuccess: () => {
          setTestResult('success');
          setTestMessage('Connection successful');
          doSave();
        },
        onError: (err) => {
          setTestResult('failure');
          setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Connection failed');
        },
      });
    } else {
      doSave();
    }
  }, [buildUpdatedSource, isNew, create, update, runTest, onModalClose]);

  return (
    <>
      <ModalHeader>Twitch</ModalHeader>

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
          <FormLabel>Client ID</FormLabel>
          <FormInputGroup
            type={inputTypes.TEXT}
            name="clientId"
            helpText="Twitch application Client ID from dev.twitch.tv/console."
            value={getVal('clientId', '')}
            errors={[]}
            warnings={[]}
            onChange={handleInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>Client Secret</FormLabel>
          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="clientSecret"
            helpText="Twitch application Client Secret from dev.twitch.tv/console."
            value={getVal('clientSecret', '')}
            errors={[]}
            warnings={[]}
            onChange={handleInputChange}
          />
        </FormGroup>

        <BaseSettingsFields
          getVal={getVal}
          onChange={handleInputChange}
          showVideos={true}
          showShorts={true}
          videosLabel="Highlights"
          shortsLabel="Clips"
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
          Save
        </SpinnerButton>
      </ModalFooter>
    </>
  );
}

const descriptor: SourceDescriptor = {
  platformConfig: {
    label: 'Twitch',
    channelPlatform: 'twitch',
    implementation: 'Twitch',
    searchPlaceholder: 'Twitch username or channel URL',
    badgeVariant: 'twitch',
    buildContentUrl: (id) =>
      id.startsWith('https://') ? id : `https://www.twitch.tv/videos/${id}`,
    videosLabel: 'Highlights',
    shortsLabel: 'Clips',
    contentTypeLabel: (ct) => {
      switch (ct) {
        case 'video': return 'Highlight';
        case 'short': return 'Clip';
        case 'vod': return 'VoD';
        case 'live': return 'Live';
        case 'upcoming': return 'Upcoming';
        default: return '';
      }
    },
    showMembershipButton: false,
  },
  SettingsForm: TwitchSourceForm,
};

export default descriptor;
