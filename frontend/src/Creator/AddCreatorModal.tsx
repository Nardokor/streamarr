import React from 'react';
import Modal from 'Components/Modal/Modal';
import { CreatorLookupResult } from 'typings/Creator';
import AddCreatorModalContent from './AddCreatorModalContent';

interface AddCreatorModalProps {
  isOpen: boolean;
  creator?: CreatorLookupResult;
  onModalClose: () => void;
  onCreatorAdded: () => void;
}

function AddCreatorModal({
  isOpen,
  creator,
  onModalClose,
  onCreatorAdded,
}: AddCreatorModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddCreatorModalContent
        creator={creator}
        onModalClose={onModalClose}
        onCreatorAdded={onCreatorAdded}
      />
    </Modal>
  );
}

export default AddCreatorModal;
