import React, { useCallback, useState } from 'react';
import { useHistory } from 'react-router';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import { useAddCreator, useCreatorLookup } from './useCreators';
import styles from './AddCreatorModalContent.css';

function CreatorAdd() {
  const history = useHistory();

  const [inputValue, setInputValue] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [path, setPath] = useState('');

  const { data: lookupResult, isFetching: isSearching } =
    useCreatorLookup(searchTerm);

  const { addCreator, isAdding } = useAddCreator();

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

  const handleAddPress = useCallback(() => {
    if (!lookupResult || !path) {
      return;
    }

    addCreator(
      {
        title: lookupResult.name,
        description: lookupResult.description,
        thumbnailUrl: lookupResult.thumbnailUrl,
        path,
        qualityProfileId: 1,
        tags: [],
        monitored: true,
      },
      {
        onSuccess: () => {
          history.push('/creators');
        },
      }
    );
  }, [lookupResult, path, addCreator, history]);

  const canAdd = !!lookupResult && path.length > 0 && !isAdding;

  return (
    <PageContent title="Add Creator">
      <PageContentBody>
        <div className={styles.searchRow}>
          <input
            className={styles.searchInput}
            type="text"
            placeholder="YouTube @handle, channel URL, or name"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onKeyDown={handleKeyDown}
            autoFocus={true}
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
          <div className={styles.result}>
            {lookupResult.thumbnailUrl ? (
              <img
                className={styles.thumbnail}
                src={lookupResult.thumbnailUrl}
                alt={lookupResult.name}
              />
            ) : null}

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
          </div>
        ) : null}

        {lookupResult ? (
          <div className={styles.pathRow}>
            <label className={styles.pathLabel} htmlFor="creator-path">
              Folder Path
            </label>

            <input
              id="creator-path"
              className={styles.searchInput}
              type="text"
              placeholder="/media/creators/creator-name"
              value={path}
              onChange={(e) => setPath(e.target.value)}
            />
          </div>
        ) : null}

        {lookupResult ? (
          <div className={styles.pathRow}>
            <SpinnerButton
              kind={kinds.PRIMARY}
              isDisabled={!canAdd}
              isSpinning={isAdding}
              onPress={handleAddPress}
            >
              Add Creator
            </SpinnerButton>
          </div>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorAdd;
