import React from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import {
  MetadataSourceResource,
  useMetadataSourceSchemas,
} from './useMetadataSources';
import styles from './Sources.css';

interface AddSourceModalProps {
  isOpen: boolean;
  configuredImplementations: string[];
  onSelect: (template: MetadataSourceResource) => void;
  onModalClose: () => void;
}

function AddSourceModal({
  isOpen,
  configuredImplementations,
  onSelect,
  onModalClose,
}: AddSourceModalProps) {
  const { data: schemas } = useMetadataSourceSchemas();

  const available = (schemas ?? []).filter(
    (s) => !configuredImplementations.includes(s.implementation)
  );

  return (
    <Modal isOpen={isOpen} size="small" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>Add Source</ModalHeader>

        <ModalBody>
          <div className={styles.platformGrid}>
            {available.map((s) => (
              <div
                key={s.implementation}
                className={styles.platformCard}
                onClick={() => onSelect(s)}
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
