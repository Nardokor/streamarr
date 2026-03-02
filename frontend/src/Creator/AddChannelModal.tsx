import React, { useCallback, useState } from 'react';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import {
  getFieldValue,
  useMetadataSources,
} from 'Settings/Sources/useMetadataSources';
import { CreatorLookupChannel } from 'typings/Creator';
import { useAddChannel, useCreatorLookup } from './useCreators';
import styles from './AddChannelModal.css';

interface AddChannelModalProps {
  isOpen: boolean;
  creatorId: number;
  onModalClose: () => void;
}

const PLATFORMS = [
  {
    id: 'youtube',
    label: 'YouTube',
    placeholder: 'YouTube @handle, channel URL, or name',
  },
  {
    id: 'twitch',
    label: 'Twitch',
    placeholder: 'Twitch username or channel URL',
  },
] as const;

function AddChannelModal({
  isOpen,
  creatorId,
  onModalClose,
}: AddChannelModalProps) {
  const { data: sources } = useMetadataSources();
  const youtubeSource = (sources ?? []).find(
    (s) => s.implementation === 'YouTube' && s.enable
  );
  const twitchSource = (sources ?? []).find(
    (s) => s.implementation === 'Twitch' && s.enable
  );

  const configuredPlatforms = PLATFORMS.filter((p) => {
    if (p.id === 'youtube') return !!youtubeSource;
    if (p.id === 'twitch') return !!twitchSource;
    return false;
  });

  const [selectedPlatformId, setSelectedPlatformId] = useState<string | null>(
    null
  );
  const [inputValue, setInputValue] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selected, setSelected] = useState<CreatorLookupChannel | null>(null);

  const activePlatform =
    configuredPlatforms.find((p) => p.id === selectedPlatformId) ??
    (configuredPlatforms.length > 0 ? configuredPlatforms[0] : null);

  const activeSource =
    activePlatform?.id === 'twitch' ? twitchSource : youtubeSource;

  const { data: lookupResult, isFetching: isSearching } =
    useCreatorLookup(searchTerm, activePlatform?.id);

  const { addChannel, isAdding } = useAddChannel(creatorId);

  const handleSearch = useCallback(() => {
    setSelected(null);
    setSearchTerm(inputValue.trim());
  }, [inputValue]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') handleSearch();
    },
    [handleSearch]
  );

  const handleAdd = useCallback(() => {
    if (!selected) return;

    addChannel(
      {
        creatorId,
        platform: selected.platform as never,
        platformId: selected.platformId,
        platformUrl: selected.platformUrl,
        title: selected.title,
        description: selected.description,
        thumbnailUrl: selected.thumbnailUrl,
        monitored: true,
        downloadVideos: getFieldValue(activeSource?.fields ?? [], 'defaultDownloadVideos', false),
        downloadShorts: getFieldValue(activeSource?.fields ?? [], 'defaultDownloadShorts', false),
        downloadVods: getFieldValue(activeSource?.fields ?? [], 'defaultDownloadVods', false),
        downloadLive: getFieldValue(activeSource?.fields ?? [], 'defaultDownloadLive', true),
        watchedWords: getFieldValue(activeSource?.fields ?? [], 'defaultWatchedWords', ''),
        ignoredWords: getFieldValue(activeSource?.fields ?? [], 'defaultIgnoredWords', ''),
        watchedDefeatsIgnored: getFieldValue(activeSource?.fields ?? [], 'defaultWatchedDefeatsIgnored', true),
        autoDownload: getFieldValue(activeSource?.fields ?? [], 'defaultAutoDownload', false),
        retentionDays: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionDays', 0) || null,
        retentionVideos: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionVideos', false),
        retentionShorts: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionShorts', false),
        retentionVods: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionVods', false),
        retentionLive: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionLive', false),
        retentionExceptionWords: getFieldValue(activeSource?.fields ?? [], 'defaultRetentionExceptionWords', ''),
      },
      {
        onSuccess: () => {
          setInputValue('');
          setSearchTerm('');
          setSelected(null);
          onModalClose();
        },
      }
    );
  }, [selected, creatorId, activeSource, addChannel, onModalClose]);

  const handleClose = useCallback(() => {
    setInputValue('');
    setSearchTerm('');
    setSelected(null);
    onModalClose();
  }, [onModalClose]);

  const handlePlatformSelect = useCallback(
    (id: string) => {
      setSelectedPlatformId(id);
      setInputValue('');
      setSearchTerm('');
      setSelected(null);
    },
    []
  );

  const channels = lookupResult?.channels ?? [];

  return (
    <Modal isOpen={isOpen} onModalClose={handleClose}>
      <ModalContent onModalClose={handleClose}>
        <ModalHeader>Add Channel</ModalHeader>

        <ModalBody>
          {configuredPlatforms.length === 0 ? (
            <p className={styles.noSources}>
              No sources configured. Go to{' '}
              <a href="/settings/sources">Settings &rsaquo; Sources</a> to add a
              platform.
            </p>
          ) : (
            <>
              <div className={styles.platformTabs}>
                {configuredPlatforms.map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    className={`${styles.platformTab} ${activePlatform?.id === p.id ? styles.platformTabActive : ''}`}
                    onClick={() => handlePlatformSelect(p.id)}
                  >
                    {p.label}
                  </button>
                ))}
              </div>

              <div className={styles.searchRow}>
                <input
                  className={styles.searchInput}
                  type="text"
                  placeholder={activePlatform?.placeholder ?? 'Search…'}
                  value={inputValue}
                  onChange={(e) => setInputValue(e.target.value)}
                  onKeyDown={handleKeyDown}
                />

                <button
                  className={styles.searchButton}
                  type="button"
                  disabled={inputValue.trim().length === 0 || isSearching}
                  onClick={handleSearch}
                >
                  {isSearching ? 'Searching…' : 'Search'}
                </button>
              </div>

              {isSearching ? <LoadingIndicator /> : null}

              {!isSearching && searchTerm && channels.length === 0 ? (
                <p className={styles.noResults}>No channels found.</p>
              ) : null}

              {channels.length > 0 ? (
                <div className={styles.channelList}>
                  {channels.map((ch) => (
                    <div
                      key={ch.platformId}
                      className={`${styles.channelOption} ${selected?.platformId === ch.platformId ? styles.channelSelected : ''}`}
                      onClick={() => setSelected(ch)}
                      role="button"
                      tabIndex={0}
                      onKeyDown={(e) => e.key === 'Enter' && setSelected(ch)}
                    >
                      {ch.thumbnailUrl ? (
                        <img
                          className={styles.channelThumb}
                          src={ch.thumbnailUrl}
                          alt={ch.title}
                        />
                      ) : null}

                      <div className={styles.channelInfo}>
                        <span className={styles.channelTitle}>{ch.title}</span>
                        <span className={styles.channelPlatform}>
                          {activePlatform?.label ?? ch.platform}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              ) : null}
            </>
          )}
        </ModalBody>

        <ModalFooter>
          <Button onPress={handleClose}>Cancel</Button>

          {configuredPlatforms.length > 0 ? (
            <SpinnerButton
              kind={kinds.PRIMARY}
              isDisabled={!selected || isAdding}
              isSpinning={isAdding}
              onPress={handleAdd}
            >
              Add Channel
            </SpinnerButton>
          ) : null}
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default AddChannelModal;
