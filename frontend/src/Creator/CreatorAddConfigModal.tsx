import React, { useCallback, useEffect, useState } from 'react';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import { RootFolder } from 'RootFolder/useRootFolders';
import { QualityProfileModel } from 'Settings/Profiles/Quality/useQualityProfiles';
import { CreatorLookupResult } from 'typings/Creator';
import { useAddCreator } from './useCreators';
import styles from './CreatorAddConfigModal.css';

interface CreatorAddConfigModalProps {
  isOpen: boolean;
  lookupResult: CreatorLookupResult | null;
  rootFolders: RootFolder[];
  qualityProfiles: QualityProfileModel[];
  onModalClose: () => void;
  onCreatorAdded: () => void;
}

function CreatorAddConfigModal({
  isOpen,
  lookupResult,
  rootFolders,
  qualityProfiles,
  onModalClose,
  onCreatorAdded,
}: CreatorAddConfigModalProps) {
  const [rootFolderId, setRootFolderId] = useState<number | undefined>(undefined);
  const [monitored, setMonitored] = useState(true);
  const [qualityProfileId, setQualityProfileId] = useState<number | undefined>(
    undefined
  );

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
          onCreatorAdded();
          onModalClose();
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
    onCreatorAdded,
    onModalClose,
  ]);

  const canAdd = !!lookupResult && rootFolderId !== undefined && !isAdding;

  if (!lookupResult) {
    return null;
  }

  const selectedRootFolder = rootFolders.find((f) => f.id === rootFolderId);
  const folderHint = selectedRootFolder
    ? `'${lookupResult.name}' subfolder will be created automatically`
    : null;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose} size="medium">
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>{lookupResult.name}</ModalHeader>

        <ModalBody>
          <div className={styles.body}>
            {lookupResult.thumbnailUrl ? (
              <img
                className={styles.avatar}
                src={lookupResult.thumbnailUrl}
                alt={lookupResult.name}
              />
            ) : (
              <div className={styles.avatarPlaceholder} />
            )}

            <div className={styles.details}>
              {lookupResult.description ? (
                <p className={styles.description}>
                  {lookupResult.description.slice(0, 400)}
                  {lookupResult.description.length > 400 ? '…' : ''}
                </p>
              ) : null}

              <div className={styles.configForm}>
                <div className={styles.configRow}>
                  <label className={styles.configLabel} htmlFor="cfg-root-folder">
                    Root Folder
                  </label>

                  <div className={styles.configField}>
                    <select
                      id="cfg-root-folder"
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

                    {folderHint ? (
                      <div className={styles.fieldHint}>{folderHint}</div>
                    ) : null}
                  </div>
                </div>

                <div className={styles.configRow}>
                  <label className={styles.configLabel} htmlFor="cfg-monitor">
                    Monitor
                  </label>

                  <div className={styles.configField}>
                    <select
                      id="cfg-monitor"
                      className={styles.configSelect}
                      value={monitored ? 'all' : 'none'}
                      onChange={(e) => setMonitored(e.target.value === 'all')}
                    >
                      <option value="all">All</option>
                      <option value="none">None</option>
                    </select>
                  </div>
                </div>

                <div className={styles.configRow}>
                  <label
                    className={styles.configLabel}
                    htmlFor="cfg-quality-profile"
                  >
                    Quality Profile
                  </label>

                  <div className={styles.configField}>
                    <select
                      id="cfg-quality-profile"
                      className={styles.configSelect}
                      value={qualityProfileId ?? ''}
                      onChange={(e) =>
                        setQualityProfileId(Number(e.target.value))
                      }
                    >
                      {qualityProfiles.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Cancel</Button>

          <SpinnerButton
            kind={kinds.PRIMARY}
            isDisabled={!canAdd}
            isSpinning={isAdding}
            onPress={handleAddPress}
          >
            Add {lookupResult.name}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default CreatorAddConfigModal;
