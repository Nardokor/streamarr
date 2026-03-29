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

function FanslySourceForm({ source, onModalClose }: SourceFormProps) {
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
      name: source.name || 'Fansly',
      enable: enabled,
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, enabled, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Connected to Fansly successfully');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Connection failed');
      },
    });
  }, [runTest, buildUpdatedSource]);

  const handleSave = useCallback(() => {
    const updated = buildUpdatedSource();
    const authToken = getFieldValue<string>(updated.fields, 'authToken', '');
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

    if (authToken && updated.enable) {
      runTest(updated, {
        onSuccess: () => {
          setTestResult('success');
          setTestMessage('Connected to Fansly successfully');
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
      <ModalHeader>Fansly</ModalHeader>

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
          <FormLabel>Auth Token</FormLabel>
          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="authToken"
            helpText="Your Fansly session token. In your browser, open DevTools → Application → Local Storage → fansly.com and copy the value of 'session_active_session', then extract the 'token' field from the JSON."
            value={getVal('authToken', '')}
            errors={[]}
            warnings={[]}
            onChange={handleInputChange}
          />
        </FormGroup>

        <BaseSettingsFields
          getVal={getVal}
          onChange={handleInputChange}
          showVideos={true}
          showShorts={false}
          showVods={false}
          showLive={true}
          showWords={false}
          videosLabel="Posts"
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
    label: 'Fansly',
    channelPlatform: 'fansly',
    implementation: 'Fansly',
    searchPlaceholder: 'Fansly URL or username (e.g. https://fansly.com/creatorname)',
    buildContentUrl: (id) => `https://fansly.com/post/${id}`,
    videosLabel: 'Posts',
    shortsLabel: 'Posts',
    contentTypeLabel: (ct) => {
      switch (ct) {
        case 'video': return 'Video';
        case 'short': return 'Post';
        case 'vod': return 'VoD';
        case 'live': return 'Live';
        case 'upcoming': return 'Upcoming';
        default: return '';
      }
    },
    showMembershipButton: false,
  },
  SettingsForm: FanslySourceForm,
};

export default descriptor;
