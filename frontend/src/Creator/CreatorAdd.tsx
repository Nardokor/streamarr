import React, { useCallback, useEffect, useState } from 'react';
import { useHistory } from 'react-router';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import useRootFolders from 'RootFolder/useRootFolders';
import { useQualityProfilesData } from 'Settings/Profiles/Quality/useQualityProfiles';
import { useAddCreator, useCreatorLookup } from './useCreators';
import styles from './AddCreatorModalContent.css';

function CreatorAdd() {
  const history = useHistory();

  const [inputValue, setInputValue] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [configuring, setConfiguring] = useState(false);
  const [rootFolderId, setRootFolderId] = useState<number | undefined>(undefined);
  const [monitored, setMonitored] = useState(true);
  const [qualityProfileId, setQualityProfileId] = useState<number | undefined>(
    undefined
  );

  const { data: lookupResult, isFetching: isSearching } =
    useCreatorLookup(searchTerm);
  const { data: rootFolders } = useRootFolders();
  const qualityProfiles = useQualityProfilesData();
  const { addCreator, isAdding } = useAddCreator();

  useEffect(() => {
    if (rootFolders.length > 0 && rootFolderId === undefined) {
      setRootFolderId(rootFolders[0].id);
    }
  }, [rootFolders, rootFolderId]);

  useEffect(() => {
    if (qualityProfiles.length > 0 && qualityProfileId === undefined) {
      setQualityProfileId(qualityProfiles[0].id);
    }
  }, [qualityProfiles, qualityProfileId]);

  useEffect(() => {
    setConfiguring(false);
  }, [lookupResult]);

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
    setConfiguring(true);
  }, []);

  const handleBack = useCallback(() => {
    setConfiguring(false);
  }, []);

  const handleAddPress = useCallback(() => {
    if (!lookupResult || rootFolderId === undefined) return;

    const rootFolder = rootFolders.find((f) => f.id === rootFolderId);
    if (!rootFolder) return;

    const base = rootFolder.path.replace(/\/+$/, '');
    const folderName = lookupResult.name.trim().replace(/[\\/:*?"<>|]/g, '');
    const path = `${base}/${folderName}`;

    addCreator(
      {
        title: lookupResult.name,
        description: lookupResult.description,
        thumbnailUrl: lookupResult.thumbnailUrl,
        path,
        qualityProfileId: qualityProfileId ?? 1,
        tags: [],
        monitored,
        channels: lookupResult.channels,
      },
      {
        onSuccess: () => {
          history.push('/creators');
        },
      }
    );
  }, [
    lookupResult,
    rootFolderId,
    rootFolders,
    qualityProfileId,
    monitored,
    addCreator,
    history,
  ]);

  const canAdd =
    !!lookupResult && rootFolderId !== undefined && !isAdding;

  return (
    <PageContent title="Add Creator">
      <PageContentBody>
        {!configuring ? (
          <>
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
              <div
                className={styles.resultCard}
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

                <div className={styles.addHint}>Click to add</div>
              </div>
            ) : null}
          </>
        ) : (
          <>
            <div className={styles.selectedCreator}>
              {lookupResult?.thumbnailUrl ? (
                <img
                  className={styles.thumbnail}
                  src={lookupResult.thumbnailUrl}
                  alt={lookupResult.name}
                />
              ) : null}

              <div className={styles.selectedName}>{lookupResult?.name}</div>
            </div>

            <div className={styles.configForm}>
              <div className={styles.configRow}>
                <label className={styles.configLabel} htmlFor="root-folder">
                  Root Folder
                </label>

                <select
                  id="root-folder"
                  className={styles.configSelect}
                  value={rootFolderId ?? ''}
                  onChange={(e) => setRootFolderId(Number(e.target.value))}
                >
                  {rootFolders.map((f) => (
                    <option key={f.id} value={f.id}>
                      {f.path}
                    </option>
                  ))}
                </select>
              </div>

              <div className={styles.configRow}>
                <label className={styles.configLabel} htmlFor="monitor">
                  Monitor
                </label>

                <select
                  id="monitor"
                  className={styles.configSelect}
                  value={monitored ? 'all' : 'none'}
                  onChange={(e) => setMonitored(e.target.value === 'all')}
                >
                  <option value="all">All</option>
                  <option value="none">None</option>
                </select>
              </div>

              <div className={styles.configRow}>
                <label className={styles.configLabel} htmlFor="quality-profile">
                  Quality Profile
                </label>

                <select
                  id="quality-profile"
                  className={styles.configSelect}
                  value={qualityProfileId ?? ''}
                  onChange={(e) => setQualityProfileId(Number(e.target.value))}
                >
                  {qualityProfiles.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className={styles.configRow} style={{ marginTop: 20 }}>
              <button
                className={styles.searchButton}
                style={{ background: 'transparent', color: 'inherit', border: '1px solid var(--borderColor, #aaa)' }}
                onClick={handleBack}
                type="button"
              >
                Back
              </button>

              <SpinnerButton
                kind={kinds.PRIMARY}
                isDisabled={!canAdd}
                isSpinning={isAdding}
                onPress={handleAddPress}
              >
                Add Creator
              </SpinnerButton>
            </div>
          </>
        )}
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorAdd;
