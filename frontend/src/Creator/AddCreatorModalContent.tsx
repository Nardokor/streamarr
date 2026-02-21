import React, { useCallback, useState } from 'react';
import { useHistory } from 'react-router';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import useRootFolders from 'RootFolder/useRootFolders';
import { useQualityProfilesData } from 'Settings/Profiles/Quality/useQualityProfiles';
import { CreatorLookupResult } from 'typings/Creator';
import CreatorAddConfigModal from './CreatorAddConfigModal';
import { useCreatorLookup } from './useCreators';
import styles from './AddCreatorModalContent.css';

interface AddCreatorModalContentProps {
  onModalClose: () => void;
  onCreatorAdded: () => void;
}

function AddCreatorModalContent({
  onModalClose,
  onCreatorAdded,
}: AddCreatorModalContentProps) {
  const history = useHistory();
  const [inputValue, setInputValue] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [configTarget, setConfigTarget] = useState<CreatorLookupResult | null>(
    null
  );

  const { data: lookupResult, isFetching: isSearching } =
    useCreatorLookup(searchTerm);
  const { data: rootFolders } = useRootFolders();
  const qualityProfiles = useQualityProfilesData();

  const handleSearchPress = useCallback(() => {
    setSearchTerm(inputValue.trim());
  }, [inputValue]);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLInputElement>) => {
      if (event.key === 'Enter') {
        setSearchTerm(inputValue.trim());
      }
    },
    [inputValue]
  );

  const handleResultClick = useCallback(() => {
    if (!lookupResult) return;

    if (lookupResult.existingCreatorId != null) {
      onModalClose();
      history.push(`/creator/${lookupResult.existingCreatorId}`);
    } else {
      setConfigTarget(lookupResult);
    }
  }, [lookupResult, onModalClose, history]);

  const handleConfigClose = useCallback(() => {
    setConfigTarget(null);
  }, []);

  const isExisting = lookupResult?.existingCreatorId != null;

  return (
    <>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>Add Creator</ModalHeader>

        <ModalBody>
          <div className={styles.searchRow}>
            <input
              className={styles.searchInput}
              type="text"
              placeholder="YouTube @handle, channel URL, or name"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyDown={handleKeyDown}
              autoFocus
            />

            <button
              className={styles.searchButton}
              disabled={inputValue.trim().length === 0 || isSearching}
              onClick={handleSearchPress}
              type="button"
            >
              {isSearching ? 'Searching…' : 'Search'}
            </button>
          </div>

          {lookupResult ? (
            <div
              className={`${styles.resultCard} ${isExisting ? styles.resultCardExisting : ''}`}
              onClick={handleResultClick}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => e.key === 'Enter' && handleResultClick()}
            >
              {lookupResult.thumbnailUrl ? (
                <img
                  className={styles.thumbnail}
                  src={lookupResult.thumbnailUrl}
                  alt={lookupResult.name}
                />
              ) : (
                <div className={styles.thumbnailPlaceholder} />
              )}

              <div className={styles.resultInfo}>
                <div className={styles.resultName}>{lookupResult.name}</div>

                {lookupResult.description ? (
                  <div className={styles.resultDescription}>
                    {lookupResult.description.slice(0, 200)}
                    {lookupResult.description.length > 200 ? '…' : ''}
                  </div>
                ) : null}

                {lookupResult.channels.length > 0 ? (
                  <div className={styles.channels}>
                    {lookupResult.channels.map((ch) => (
                      <span key={ch.platformId} className={styles.channelBadge}>
                        {ch.platform}: {ch.title}
                      </span>
                    ))}
                  </div>
                ) : null}
              </div>

              <div className={isExisting ? styles.existingHint : styles.addHint}>
                {isExisting ? 'Already added — click to view' : 'Click to add'}
              </div>
            </div>
          ) : null}
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Cancel</Button>
        </ModalFooter>
      </ModalContent>

      <CreatorAddConfigModal
        isOpen={!!configTarget}
        lookupResult={configTarget}
        rootFolders={rootFolders}
        qualityProfiles={qualityProfiles}
        onModalClose={handleConfigClose}
        onCreatorAdded={onCreatorAdded}
      />
    </>
  );
}

export default AddCreatorModalContent;
