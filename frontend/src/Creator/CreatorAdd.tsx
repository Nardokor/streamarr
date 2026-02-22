import React, { useCallback, useEffect, useState } from 'react';
import { useHistory } from 'react-router';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import useRootFolders from 'RootFolder/useRootFolders';
import { useQualityProfilesData } from 'Settings/Profiles/Quality/useQualityProfiles';
import { useAddCreator } from './useCreators';
import styles from './AddCreatorModalContent.css';

function sanitize(name: string): string {
  return name.trim().replace(/[\\/:*?"<>|]/g, '');
}

function CreatorAdd() {
  const history = useHistory();
  const [name, setName] = useState('');
  const [rootFolderId, setRootFolderId] = useState<number | undefined>();

  const { data: rootFolders } = useRootFolders();
  const qualityProfiles = useQualityProfilesData();
  const { addCreator, isAdding } = useAddCreator();

  useEffect(() => {
    if (rootFolders.length > 0 && rootFolderId === undefined) {
      setRootFolderId(rootFolders[0].id);
    }
  }, [rootFolders, rootFolderId]);

  const selectedFolder = rootFolders.find((f) => f.id === rootFolderId);
  const folderName = sanitize(name);
  const fullPath =
    selectedFolder && folderName
      ? `${selectedFolder.path.replace(/\/+$/, '')}/${folderName}`
      : '';

  const canAdd = !!folderName && rootFolderId !== undefined && !isAdding;

  const handleAdd = useCallback(() => {
    if (!canAdd || !fullPath) {
      return;
    }

    addCreator(
      {
        title: name.trim(),
        path: fullPath,
        qualityProfileId: qualityProfiles[0]?.id ?? 1,
        monitored: true,
        channels: [],
        tags: [],
      },
      {
        onSuccess: () => {
          history.push('/creator');
        },
      }
    );
  }, [canAdd, fullPath, name, qualityProfiles, addCreator, history]);

  return (
    <PageContent title="Add Creator">
      <PageContentBody>
        <div className={styles.form}>
          <div className={styles.formRow}>
            <label className={styles.label} htmlFor="creator-name">
              Name
            </label>

            <input
              id="creator-name"
              className={styles.input}
              type="text"
              placeholder="Creator name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && canAdd && handleAdd()}
              autoFocus
            />
          </div>

          <div className={styles.formRow}>
            <label className={styles.label} htmlFor="creator-root">
              Root Folder
            </label>

            <div className={styles.field}>
              <select
                id="creator-root"
                className={styles.select}
                value={rootFolderId ?? ''}
                onChange={(e) => setRootFolderId(Number(e.target.value))}
              >
                {rootFolders.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.path}
                  </option>
                ))}
              </select>

              {fullPath ? <div className={styles.hint}>{fullPath}</div> : null}
            </div>
          </div>

          <div className={styles.formRow}>
            <div className={styles.label} />

            <div className={styles.field}>
              <SpinnerButton
                kind={kinds.PRIMARY}
                isDisabled={!canAdd}
                isSpinning={isAdding}
                onPress={handleAdd}
              >
                Add Creator
              </SpinnerButton>

              <Button onPress={() => history.push('/creator')}>Cancel</Button>
            </div>
          </div>
        </div>
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorAdd;
