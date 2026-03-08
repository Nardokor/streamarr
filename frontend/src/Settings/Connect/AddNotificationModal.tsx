import React from 'react';
import AddProviderCard from 'Components/AddProviderCard';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import NotificationResource from 'typings/Notification';
import { useNotificationSchema } from './useConnectSettings';
import styles from './Connect.css';

interface AddNotificationModalProps {
  isOpen: boolean;
  onSelect: (schema: NotificationResource) => void;
  onModalClose: () => void;
}

function AddNotificationModal({
  isOpen,
  onSelect,
  onModalClose,
}: AddNotificationModalProps) {
  const { data: schemas } = useNotificationSchema();

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>Add Notification</ModalHeader>

        <ModalBody>
          <div className={styles.providerGrid}>
            {(schemas ?? []).map((s) => (
              <AddProviderCard
                key={s.implementation}
                implementationName={s.implementationName}
                infoLink={s.infoLink}
                onPress={() => {
                  onSelect(s);
                  onModalClose();
                }}
              />
            ))}
          </div>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Close</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default AddNotificationModal;
