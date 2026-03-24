import React, { useCallback, useMemo, useState } from 'react';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import {
  useMetadataSources,
  MetadataSourceResource,
} from 'Settings/Sources/useMetadataSources';
import { getShortsLabel, getVideosLabel } from 'Creator/creatorUtils';
import Channel from 'typings/Channel';
import styles from './BulkEditChannelModal.css';

type Tristate = 'unchanged' | 'true' | 'false';

const PLATFORM_IMPLEMENTATION: Record<string, string> = {
  youTube: 'YouTube',
  twitch: 'Twitch',
  fourthwall: 'Fourthwall',
};

function getSourceFields(
  sources: MetadataSourceResource[] | undefined,
  platform: string
): Set<string> {
  const impl = PLATFORM_IMPLEMENTATION[platform];
  const source = (sources ?? []).find((s) => s.implementation === impl && s.enable);
  return new Set((source?.fields ?? []).map((f) => f.name));
}

function tristateToBoolean(v: Tristate): boolean | undefined {
  if (v === 'true') return true;
  if (v === 'false') return false;
  return undefined;
}

function TristateSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: Tristate;
  onChange: (v: Tristate) => void;
}) {
  return (
    <div className={styles.field}>
      <label className={styles.label}>{label}</label>
      <select
        className={styles.select}
        value={value}
        onChange={(e) => onChange(e.target.value as Tristate)}
      >
        <option value="unchanged">— no change —</option>
        <option value="true">Yes</option>
        <option value="false">No</option>
      </select>
    </div>
  );
}

interface BulkEditChannelModalProps {
  isOpen: boolean;
  selectedChannels: Channel[];
  onSave: (patch: Partial<Channel>) => void;
  isSaving: boolean;
  onModalClose: () => void;
}

function BulkEditChannelModal({
  isOpen,
  selectedChannels,
  onSave,
  isSaving,
  onModalClose,
}: BulkEditChannelModalProps) {
  const { data: sources } = useMetadataSources();

  const platforms = useMemo(
    () => [...new Set(selectedChannels.map((c) => c.platform))],
    [selectedChannels]
  );
  const isSinglePlatform = platforms.length === 1;
  const singlePlatform = isSinglePlatform ? platforms[0] : null;
  const sourceFields = useMemo(
    () => (singlePlatform ? getSourceFields(sources, singlePlatform) : new Set<string>()),
    [sources, singlePlatform]
  );
  const hasField = (name: string) => sourceFields.has(name);
  const videosLabel = singlePlatform ? getVideosLabel(singlePlatform) : 'Videos';
  const shortsLabel = singlePlatform ? getShortsLabel(singlePlatform) : 'Shorts / Clips';

  const [monitored, setMonitored] = useState<Tristate>('unchanged');
  const [autoDownload, setAutoDownload] = useState<Tristate>('unchanged');
  const [downloadVideos, setDownloadVideos] = useState<Tristate>('unchanged');
  const [downloadShorts, setDownloadShorts] = useState<Tristate>('unchanged');
  const [downloadVods, setDownloadVods] = useState<Tristate>('unchanged');
  const [downloadLive, setDownloadLive] = useState<Tristate>('unchanged');
  const [downloadMembers, setDownloadMembers] = useState<Tristate>('unchanged');
  const [watchedWords, setWatchedWords] = useState('');
  const [ignoredWords, setIgnoredWords] = useState('');
  const [watchedDefeatsIgnored, setWatchedDefeatsIgnored] = useState<Tristate>('unchanged');
  const [retentionDays, setRetentionDays] = useState('');
  const [keepVideos, setKeepVideos] = useState<Tristate>('unchanged');
  const [keepShorts, setKeepShorts] = useState<Tristate>('unchanged');
  const [keepVods, setKeepVods] = useState<Tristate>('unchanged');
  const [keepMembers, setKeepMembers] = useState<Tristate>('unchanged');
  const [retentionKeepWords, setRetentionKeepWords] = useState('');

  const handleReset = useCallback(() => {
    setMonitored('unchanged');
    setAutoDownload('unchanged');
    setDownloadVideos('unchanged');
    setDownloadShorts('unchanged');
    setDownloadVods('unchanged');
    setDownloadLive('unchanged');
    setDownloadMembers('unchanged');
    setWatchedWords('');
    setIgnoredWords('');
    setWatchedDefeatsIgnored('unchanged');
    setRetentionDays('');
    setKeepVideos('unchanged');
    setKeepShorts('unchanged');
    setKeepVods('unchanged');
    setKeepMembers('unchanged');
    setRetentionKeepWords('');
  }, []);

  const handleClose = useCallback(() => {
    handleReset();
    onModalClose();
  }, [handleReset, onModalClose]);

  const handleSave = useCallback(() => {
    const patch: Partial<Channel> = {};

    const monitoredVal = tristateToBoolean(monitored);
    if (monitoredVal !== undefined) patch.monitored = monitoredVal;

    const autoDownloadVal = tristateToBoolean(autoDownload);
    if (autoDownloadVal !== undefined) patch.autoDownload = autoDownloadVal;

    if (isSinglePlatform) {
      const dlVideosVal = tristateToBoolean(downloadVideos);
      if (dlVideosVal !== undefined) patch.downloadVideos = dlVideosVal;

      const dlShortsVal = tristateToBoolean(downloadShorts);
      if (dlShortsVal !== undefined) patch.downloadShorts = dlShortsVal;

      const dlVodsVal = tristateToBoolean(downloadVods);
      if (dlVodsVal !== undefined) patch.downloadVods = dlVodsVal;

      const dlLiveVal = tristateToBoolean(downloadLive);
      if (dlLiveVal !== undefined) patch.downloadLive = dlLiveVal;

      const dlMembersVal = tristateToBoolean(downloadMembers);
      if (dlMembersVal !== undefined) patch.downloadMembers = dlMembersVal;

      const keepVideosVal = tristateToBoolean(keepVideos);
      if (keepVideosVal !== undefined) patch.keepVideos = keepVideosVal;

      const keepShortsVal = tristateToBoolean(keepShorts);
      if (keepShortsVal !== undefined) patch.keepShorts = keepShortsVal;

      const keepVodsVal = tristateToBoolean(keepVods);
      if (keepVodsVal !== undefined) patch.keepVods = keepVodsVal;

      const keepMembersVal = tristateToBoolean(keepMembers);
      if (keepMembersVal !== undefined) patch.keepMembers = keepMembersVal;
    }

    if (watchedWords.trim() !== '') patch.watchedWords = watchedWords.trim();
    if (ignoredWords.trim() !== '') patch.ignoredWords = ignoredWords.trim();

    const wdiVal = tristateToBoolean(watchedDefeatsIgnored);
    if (wdiVal !== undefined) patch.watchedDefeatsIgnored = wdiVal;

    if (retentionDays.trim() !== '') {
      const parsed = parseInt(retentionDays.trim(), 10);
      patch.retentionDays = Number.isNaN(parsed) ? null : parsed;
    }

    if (retentionKeepWords.trim() !== '') patch.retentionKeepWords = retentionKeepWords.trim();

    onSave(patch);
  }, [
    monitored, autoDownload, isSinglePlatform,
    downloadVideos, downloadShorts, downloadVods, downloadLive, downloadMembers,
    watchedWords, ignoredWords, watchedDefeatsIgnored,
    retentionDays, keepVideos, keepShorts, keepVods, keepMembers, retentionKeepWords,
    onSave,
  ]);

  return (
    <Modal isOpen={isOpen} onModalClose={handleClose}>
      <ModalContent onModalClose={handleClose}>
        <ModalHeader>
          Bulk Edit {selectedChannels.length} Channel{selectedChannels.length !== 1 ? 's' : ''}
        </ModalHeader>

        <ModalBody>
          <p className={styles.hint}>
            Only fields you change will be applied. Leave fields as{' '}
            <em>no change</em> or blank to skip them.
          </p>

          <div className={styles.section}>
            <div className={styles.sectionTitle}>General</div>
            <TristateSelect label="Monitored" value={monitored} onChange={setMonitored} />
            <TristateSelect label="Auto Download" value={autoDownload} onChange={setAutoDownload} />
          </div>

          {isSinglePlatform ? (
            <div className={styles.section}>
              <div className={styles.sectionTitle}>Content Types</div>
              {hasField('defaultDownloadVideos') ? (
                <TristateSelect
                  label={`Download ${videosLabel}`}
                  value={downloadVideos}
                  onChange={setDownloadVideos}
                />
              ) : null}
              {hasField('defaultDownloadShorts') ? (
                <TristateSelect
                  label={`Download ${shortsLabel}`}
                  value={downloadShorts}
                  onChange={setDownloadShorts}
                />
              ) : null}
              {hasField('defaultDownloadVods') ? (
                <TristateSelect label="Download VODs" value={downloadVods} onChange={setDownloadVods} />
              ) : null}
              {hasField('defaultDownloadLive') ? (
                <TristateSelect label="Download Live" value={downloadLive} onChange={setDownloadLive} />
              ) : null}
              {hasField('defaultDownloadMembers') ? (
                <TristateSelect label="Download Members" value={downloadMembers} onChange={setDownloadMembers} />
              ) : null}
            </div>
          ) : (
            <div className={styles.mixedNote}>
              Content type settings are platform-specific and cannot be bulk-edited across mixed platforms.
              Select channels from a single platform to edit content types.
            </div>
          )}

          <div className={styles.section}>
            <div className={styles.sectionTitle}>Word Filters</div>
            <div className={styles.field}>
              <label className={styles.label}>Watched Words</label>
              <input
                className={styles.input}
                type="text"
                placeholder="Leave blank to keep existing"
                value={watchedWords}
                onChange={(e) => setWatchedWords(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.label}>Ignored Words</label>
              <input
                className={styles.input}
                type="text"
                placeholder="Leave blank to keep existing"
                value={ignoredWords}
                onChange={(e) => setIgnoredWords(e.target.value)}
              />
            </div>
            <TristateSelect
              label="Watched Defeats Ignored"
              value={watchedDefeatsIgnored}
              onChange={setWatchedDefeatsIgnored}
            />
          </div>

          <div className={styles.section}>
            <div className={styles.sectionTitle}>Retention</div>
            <div className={styles.field}>
              <label className={styles.label}>Retention Days</label>
              <input
                className={styles.input}
                type="number"
                min={0}
                placeholder="Leave blank to keep existing"
                value={retentionDays}
                onChange={(e) => setRetentionDays(e.target.value)}
              />
            </div>
            {isSinglePlatform ? (
              <>
                {hasField('defaultKeepVideos') ? (
                  <TristateSelect
                    label={`Keep ${videosLabel}`}
                    value={keepVideos}
                    onChange={setKeepVideos}
                  />
                ) : null}
                {hasField('defaultKeepShorts') ? (
                  <TristateSelect
                    label={`Keep ${shortsLabel}`}
                    value={keepShorts}
                    onChange={setKeepShorts}
                  />
                ) : null}
                {hasField('defaultKeepVods') ? (
                  <TristateSelect label="Keep VODs" value={keepVods} onChange={setKeepVods} />
                ) : null}
                {hasField('defaultKeepMembers') ? (
                  <TristateSelect label="Keep Members" value={keepMembers} onChange={setKeepMembers} />
                ) : null}
              </>
            ) : null}
            <div className={styles.field}>
              <label className={styles.label}>Retention Keep Words</label>
              <input
                className={styles.input}
                type="text"
                placeholder="Leave blank to keep existing"
                value={retentionKeepWords}
                onChange={(e) => setRetentionKeepWords(e.target.value)}
              />
            </div>
          </div>
        </ModalBody>

        <ModalFooter>
          <Button onPress={handleClose}>Cancel</Button>
          <SpinnerButton
            kind={kinds.PRIMARY}
            isSpinning={isSaving}
            isDisabled={isSaving}
            onPress={handleSave}
          >
            Apply to {selectedChannels.length} Channel{selectedChannels.length !== 1 ? 's' : ''}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default BulkEditChannelModal;
