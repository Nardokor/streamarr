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
import { kinds } from 'Helpers/Props';
import {
  MetadataSourceResource,
  applyFieldChanges,
  getFieldValue,
  useCreateMetadataSource,
  useDeleteMetadataSource,
  useMetadataSources,
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
  showVideos,
  showShorts,
  showVods = true,
  showLive = true,
  showFilters = true,
  videosLabel = 'Videos',
  shortsLabel = 'Shorts',
}: {
  getVal: <T>(name: string, fallback: T) => T;
  onChange: (change: InputChanged) => void;
  showVideos: boolean;
  showShorts: boolean;
  showVods?: boolean;
  showLive?: boolean;
  showFilters?: boolean;
  videosLabel?: string;
  shortsLabel?: string;
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
          value={getVal('refreshIntervalHours', 1)}
          errors={[]}
          warnings={
            getVal('refreshIntervalHours', 1) > 6
              ? [{ message: 'Values above 6 hours may cause recent uploads and live streams to be missed' }]
              : []
          }
          onChange={onChange}
        />
      </FormGroup>

      {showVideos && (
        <FormGroup>
          <FormLabel>Default: Download {videosLabel}</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadVideos"
            helpText={`Include ${videosLabel.toLowerCase()} by default for new channels`}
            value={getVal('defaultDownloadVideos', true)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showShorts && (
        <FormGroup>
          <FormLabel>Default: Download {shortsLabel}</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="defaultDownloadShorts"
            helpText={`Include ${shortsLabel.toLowerCase()} by default for new channels`}
            value={getVal('defaultDownloadShorts', true)}
            errors={[]}
            warnings={[]}
            onChange={onChange}
          />
        </FormGroup>
      )}

      {showVods && (
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
      )}

      {showLive && (
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
      )}

      {showFilters && (
        <>
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
      )}
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
  const isNew = !source.id;

  const [enabled, setEnabled] = useState(source.enable ?? true);
  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(
    source.id ?? 0
  );
  const isSaving = isCreating || isUpdating;
  const { mutate: deleteSource, isPending: isDeleting } =
    useDeleteMetadataSource(source.id ?? 0);


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

    if (apiKey && updated.enable) {
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
          <FormLabel>Enable</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="enable"
            helpText="Enable this source for content syncing and channel searches."
            value={enabled}
            errors={[]}
            warnings={[]}
            onChange={(change: InputChanged) =>
              setEnabled(change.value as boolean)
            }
          />
        </FormGroup>

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

        <FormGroup>
          <FormLabel>Webhook Base URL</FormLabel>
          <FormInputGroup
            type={inputTypes.TEXT}
            name="webhookBaseUrl"
            helpText="Public base URL for push notifications via Tailscale Funnel (e.g. https://shigure.taila9c4.ts.net). Leave empty to use polling only."
            value={getVal('webhookBaseUrl', '')}
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

// ── Fourthwall form ────────────────────────────────────────────────────────

function FourthwallSourceForm({
  source,
  onModalClose,
}: {
  source: MetadataSourceResource;
  onModalClose: () => void;
}) {
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
  const { mutate: deleteSource, isPending: isDeleting } =
    useDeleteMetadataSource(source.id ?? 0);


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
            onChange={(change: InputChanged) =>
              setEnabled(change.value as boolean)
            }
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

// ── Twitch form ────────────────────────────────────────────────────────────

function TwitchSourceForm({
  source,
  onModalClose,
}: {
  source: MetadataSourceResource;
  onModalClose: () => void;
}) {
  const isNew = !source.id;

  const [enabled, setEnabled] = useState(source.enable ?? true);
  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(
    source.id ?? 0
  );
  const isSaving = isCreating || isUpdating;
  const { mutate: deleteSource, isPending: isDeleting } =
    useDeleteMetadataSource(source.id ?? 0);

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

    if (clientId && clientSecret && updated.enable) {
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
          <FormLabel>Enable</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="enable"
            helpText="Enable this source for content syncing and channel searches."
            value={enabled}
            errors={[]}
            warnings={[]}
            onChange={(change: InputChanged) =>
              setEnabled(change.value as boolean)
            }
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
        ) : source?.implementation === 'Fourthwall' ? (
          <FourthwallSourceForm source={source} onModalClose={onModalClose} />
        ) : null}
      </ModalContent>
    </Modal>
  );
}

export default EditSourceModal;
