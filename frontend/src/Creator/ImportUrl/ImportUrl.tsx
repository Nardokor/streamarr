import React, { useCallback, useMemo, useState } from 'react';
import Alert from 'Components/Alert';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import useCreators, { useAllChannels } from 'Creator/useCreators';
import { kinds } from 'Helpers/Props';
import useImportUrl, { UrlImportResult } from './useImportUrl';
import styles from './ImportUrl.css';

function ImportUrl() {
  const [url, setUrl] = useState('');
  const [selectedChannelId, setSelectedChannelId] = useState<number | null>(
    null
  );
  const [lastResult, setLastResult] = useState<UrlImportResult | null>(null);

  const { importUrl, isImporting } = useImportUrl();
  const { data: creators } = useCreators();
  const { data: channels } = useAllChannels();

  const creatorTitleById = useMemo(() => {
    const map = new Map<number, string>();
    creators.forEach((c) => map.set(c.id, c.title));
    return map;
  }, [creators]);

  const handleSubmit = useCallback(() => {
    const trimmed = url.trim();
    if (!trimmed) {
      return;
    }

    importUrl(
      { url: trimmed, channelId: selectedChannelId ?? undefined },
      {
        onSuccess: (result) => {
          setLastResult(result);
          if (
            result.status === 'started' ||
            result.status === 'alreadyDownloaded'
          ) {
            setUrl('');
            setSelectedChannelId(null);
          }
        },
      }
    );
  }, [url, selectedChannelId, importUrl]);

  const handleUrlChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setUrl(e.target.value);
      setLastResult(null);
      setSelectedChannelId(null);
    },
    []
  );

  const handleUrlKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        handleSubmit();
      }
    },
    [handleSubmit]
  );

  const handleChannelSelect = useCallback(
    (e: React.ChangeEvent<HTMLSelectElement>) => {
      setSelectedChannelId(e.target.value ? Number(e.target.value) : null);
    },
    []
  );

  const needsTarget = lastResult?.status === 'needsTarget';

  return (
    <PageContent title="Import URL">
      <PageContentBody>
        <div className={styles.container}>
          <p className={styles.helpText}>
            Paste a video URL to download it directly — useful for unlisted
            members-only videos that normal channel scans can&apos;t find. If
            the video&apos;s channel is already configured, it&apos;s added
            straight to that creator&apos;s library.
          </p>

          <div className={styles.inputRow}>
            <input
              className={styles.input}
              type="text"
              placeholder="https://www.youtube.com/watch?v=..."
              value={url}
              onChange={handleUrlChange}
              onKeyDown={handleUrlKeyDown}
            />

            <SpinnerButton
              kind={kinds.PRIMARY}
              isDisabled={url.trim().length === 0 || isImporting}
              isSpinning={isImporting}
              onPress={handleSubmit}
            >
              Import
            </SpinnerButton>
          </div>

          {lastResult && !needsTarget ? (
            <Alert
              kind={
                lastResult.status === 'error' ? kinds.DANGER : kinds.SUCCESS
              }
              className={styles.resultAlert}
            >
              {lastResult.message}
            </Alert>
          ) : null}

          {needsTarget ? (
            <div className={styles.targetPicker}>
              <Alert kind={kinds.WARNING} className={styles.resultAlert}>
                {lastResult.message}
              </Alert>

              {lastResult.resolvedTitle ? (
                <p className={styles.resolvedTitle}>
                  Resolved: <strong>{lastResult.resolvedTitle}</strong>
                  {lastResult.resolvedChannelTitle
                    ? ` (${lastResult.resolvedChannelTitle})`
                    : null}
                </p>
              ) : null}

              <select
                className={styles.channelSelect}
                value={selectedChannelId ?? ''}
                onChange={handleChannelSelect}
              >
                <option value="">Select a creator/channel…</option>
                {channels.map((ch) => (
                  <option key={ch.id} value={ch.id}>
                    {creatorTitleById.get(ch.creatorId) ?? 'Unknown'} —{' '}
                    {ch.title} ({ch.platform})
                  </option>
                ))}
              </select>

              <SpinnerButton
                kind={kinds.PRIMARY}
                isDisabled={!selectedChannelId || isImporting}
                isSpinning={isImporting}
                onPress={handleSubmit}
              >
                Import to Selected Channel
              </SpinnerButton>
            </div>
          ) : null}
        </div>
      </PageContentBody>
    </PageContent>
  );
}

export default ImportUrl;
