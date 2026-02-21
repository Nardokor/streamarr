import React from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import styles from './Sources.css';

const ALL_SOURCES = [{ id: 'youtube', name: 'YouTube' }];

interface AddSourceModalProps {
  isOpen: boolean;
  configuredSources: string[];
  onSelect: (source: string) => void;
  onModalClose: () => void;
}

function AddSourceModal({
  isOpen,
  configuredSources,
  onSelect,
  onModalClose,
}: AddSourceModalProps) {
  const available = ALL_SOURCES.filter((s) => !configuredSources.includes(s.id));

  return (
    <Modal isOpen={isOpen} size="small" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>Add Source</ModalHeader>

        <ModalBody>
          <div className={styles.platformGrid}>
            {available.map((s) => (
              <div
                key={s.id}
                className={styles.platformCard}
                onClick={() => onSelect(s.id)}
              >
                {s.name}
              </div>
            ))}

            {available.length === 0 && <p>All sources are configured.</p>}
          </div>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Close</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default AddSourceModal;
