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
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import {
  MetadataSourceResource,
  applyFieldChanges,
  getFieldValue,
  useCreateMetadataSource,
  useTestMetadataSource,
  useUpdateMetadataSource,
} from './useMetadataSources';

interface EditSourceModalProps {
  source: MetadataSourceResource | null;
  isOpen: boolean;
  onModalClose: () => void;
}

// ── Shared base settings fields (interval + channel defaults) ──────────────

function BaseSettingsFields({
  getVal,
  onChange,
  showVideoShorts,
}: {
  getVal: <T>(name: string, fallback: T) => T;
  onChange: (change: InputChanged) => void;
  showVideoShorts: boolean;
}) {
  return (
    <>
      <FormGroup>
        <FormLabel>Full Refresh Interval (hours)</FormLabel>
        <FormInputGroup
          type={inputTypes.NUMBER}
          name="refreshIntervalHours"
          helpText="How often to scan for new content (min 1, max 168)"
          min={1}
          max={168}
          value={getVal('refreshIntervalHours', 24)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Live Check Interval (minutes)</FormLabel>
        <FormInputGroup
          type={inputTypes.NUMBER}
          name="liveCheckIntervalMinutes"
          helpText="How often to check livestream status (min 5, max 1440)"
          min={5}
          max={1440}
          value={getVal('liveCheckIntervalMinutes', 60)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      {showVideoShorts && (
        <>
          <FormGroup>
            <FormLabel>Default: Download Videos</FormLabel>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="defaultDownloadVideos"
              helpText="Include regular videos by default for new channels"
              value={getVal('defaultDownloadVideos', true)}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>Default: Download Shorts</FormLabel>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="defaultDownloadShorts"
              helpText="Include shorts by default for new channels"
              value={getVal('defaultDownloadShorts', true)}
              errors={[]}
              warnings={[]}
              onChange={onChange}
            />
          </FormGroup>
        </>
      )}

      <FormGroup>
        <FormLabel>Default: Download VoDs</FormLabel>
        <FormInputGroup
          type={inputTypes.CHECK}
          name="defaultDownloadVods"
          helpText="Include past livestreams by default for new channels"
          value={getVal('defaultDownloadVods', true)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Default: Download Live</FormLabel>
        <FormInputGroup
          type={inputTypes.CHECK}
          name="defaultDownloadLive"
          helpText="Record active livestreams by default for new channels"
          value={getVal('defaultDownloadLive', false)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Default: Watched Words</FormLabel>
        <FormInputGroup
          type={inputTypes.TEXT}
          name="defaultWatchedWords"
          helpText="word1, word2 … — only matching content is wanted (blank = all)"
          value={getVal('defaultWatchedWords', '')}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Default: Ignored Words</FormLabel>
        <FormInputGroup
          type={inputTypes.TEXT}
          name="defaultIgnoredWords"
          helpText="word1, word2 … — matching content is unwanted (blank = none)"
          value={getVal('defaultIgnoredWords', '')}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Default: Watched Defeats Ignored</FormLabel>
        <FormInputGroup
          type={inputTypes.CHECK}
          name="defaultWatchedDefeatsIgnored"
          helpText="Watched words take priority over ignored words"
          value={getVal('defaultWatchedDefeatsIgnored', true)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>Default: Auto Download</FormLabel>
        <FormInputGroup
          type={inputTypes.CHECK}
          name="defaultAutoDownload"
          helpText="Automatically queue missing content for download"
          value={getVal('defaultAutoDownload', true)}
          errors={[]}
          warnings={[]}
          onChange={onChange}
        />
      </FormGroup>
    </>
  );
}

// ── YouTube form ───────────────────────────────────────────────────────────

function YouTubeSourceForm({
  source,
  onModalClose,
}: {
  source: MetadataSourceResource;
  onModalClose: () => void;
}) {
  const isNew = source.id === 0;

  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(
    source.id
  );
  const isSaving = isCreating || isUpdating;

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
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
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
    });
  }, [runTest, buildUpdatedSource]);

  const handleSave = useCallback(() => {
    const updated = buildUpdatedSource();
    const apiKey = getFieldValue<string>(updated.fields, 'apiKey', '');
    const save = isNew ? create : update;

    const doSave = () => {
      save(updated, {
        onSuccess: () => onModalClose(),
        onError: (err) => {
          setTestResult('failure');
          setTestMessage(
            err.statusBody?.message ?? err.statusText ?? 'Save failed'
          );
        },
      });
    };

    if (apiKey) {
      runTest(updated, {
        onSuccess: () => {
          setTestResult('success');
          setTestMessage('Connection successful');
          doSave();
        },
        onError: (err) => {
          setTestResult('failure');
          setTestMessage(
            err.statusBody?.message ?? err.statusText ?? 'Connection failed'
          );
        },
      });
    } else {
      doSave();
    }
  }, [buildUpdatedSource, isNew, create, update, runTest, onModalClose]);

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
            value={getVal('apiKey', '')}
            errors={[]}
            warnings={[]}
            onChange={handleInputChange}
          />
        </FormGroup>

        <BaseSettingsFields
          getVal={getVal}
          onChange={handleInputChange}
          showVideoShorts={true}
        />

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

// ── Twitch form ────────────────────────────────────────────────────────────

function TwitchSourceForm({
  source,
  onModalClose,
}: {
  source: MetadataSourceResource;
  onModalClose: () => void;
}) {
  const isNew = source.id === 0;

  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(
    source.id
  );
  const isSaving = isCreating || isUpdating;

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
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
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
          setTestMessage(
            err.statusBody?.message ?? err.statusText ?? 'Save failed'
          );
        },
      });
    };

    if (clientId && clientSecret) {
      runTest(updated, {
        onSuccess: () => {
          setTestResult('success');
          setTestMessage('Connection successful');
          doSave();
        },
        onError: (err) => {
          setTestResult('failure');
          setTestMessage(
            err.statusBody?.message ?? err.statusText ?? 'Connection failed'
          );
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
          showVideoShorts={false}
        />

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

// ── Router ─────────────────────────────────────────────────────────────────

function EditSourceModal({
  source,
  isOpen,
  onModalClose,
}: EditSourceModalProps) {
  return (
    <Modal isOpen={isOpen} size="medium" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        {source?.implementation === 'YouTube' ? (
          <YouTubeSourceForm source={source} onModalClose={onModalClose} />
        ) : source?.implementation === 'Twitch' ? (
          <TwitchSourceForm source={source} onModalClose={onModalClose} />
        ) : null}
      </ModalContent>
    </Modal>
  );
}

export default EditSourceModal;
