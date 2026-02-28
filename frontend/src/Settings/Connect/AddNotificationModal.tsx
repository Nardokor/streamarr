import React from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
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
  const { data: schema, isLoading } = useNotificationSchema();

  return (
    <Modal isOpen={isOpen} size="medium" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>Add Notification</ModalHeader>

        <ModalBody>
          {isLoading ? (
            <LoadingIndicator />
          ) : (
            <div className={styles.schemaList}>
              {(schema ?? []).map((s) => (
                <div
                  key={s.implementation}
                  className={styles.schemaItem}
                  onClick={() => {
                    onSelect(s);
                    onModalClose();
                  }}
                >
                  <div className={styles.schemaName}>{s.implementationName}</div>
                </div>
              ))}
            </div>
          )}
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Cancel</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default AddNotificationModal;
