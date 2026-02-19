import React from 'react';
import Modal from 'Components/Modal/Modal';
import AddCreatorModalContent from './AddCreatorModalContent';

interface AddCreatorModalProps {
  isOpen: boolean;
  onModalClose: () => void;
  onCreatorAdded: () => void;
}

function AddCreatorModal({
  isOpen,
  onModalClose,
  onCreatorAdded,
}: AddCreatorModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddCreatorModalContent
        onModalClose={onModalClose}
        onCreatorAdded={onCreatorAdded}
      />
    </Modal>
  );
}

export default AddCreatorModal;
