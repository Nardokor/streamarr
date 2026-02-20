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
import { CreatorLookupChannel } from 'typings/Creator';
import { useAddChannel, useCreatorLookup } from './useCreators';
import styles from './AddChannelModal.css';

interface AddChannelModalProps {
  isOpen: boolean;
  creatorId: number;
  onModalClose: () => void;
}

function platformLabel(platform: string): string {
  const map: Record<string, string> = {
    youTube: 'YouTube',
    twitch: 'Twitch',
  };
  return map[platform] ?? platform;
}

function AddChannelModal({
  isOpen,
  creatorId,
  onModalClose,
}: AddChannelModalProps) {
  const [inputValue, setInputValue] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selected, setSelected] = useState<CreatorLookupChannel | null>(null);

  const { data: lookupResult, isFetching: isSearching } =
    useCreatorLookup(searchTerm);

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
        downloadVideos: true,
        downloadShorts: true,
        downloadLivestreams: true,
        titleFilter: '',
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
  }, [selected, creatorId, addChannel, onModalClose]);

  const handleClose = useCallback(() => {
    setInputValue('');
    setSearchTerm('');
    setSelected(null);
    onModalClose();
  }, [onModalClose]);

  const channels = lookupResult?.channels ?? [];

  return (
    <Modal isOpen={isOpen} onModalClose={handleClose}>
      <ModalContent onModalClose={handleClose}>
        <ModalHeader>Add Channel</ModalHeader>

        <ModalBody>
          <div className={styles.searchRow}>
            <input
              className={styles.searchInput}
              type="text"
              placeholder="YouTube @handle, channel URL, or name"
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
                      {platformLabel(ch.platform)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : null}
        </ModalBody>

        <ModalFooter>
          <Button onPress={handleClose}>Cancel</Button>

          <SpinnerButton
            kind={kinds.PRIMARY}
            isDisabled={!selected || isAdding}
            isSpinning={isAdding}
            onPress={handleAdd}
          >
            Add Channel
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default AddChannelModal;
