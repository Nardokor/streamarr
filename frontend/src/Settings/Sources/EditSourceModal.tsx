import React from 'react';
import Modal from 'Components/Modal/Modal';
import ModalContent from 'Components/Modal/ModalContent';
import { SOURCE_REGISTRY } from 'Sources/registry';
import { MetadataSourceResource } from './useMetadataSources';

interface EditSourceModalProps {
  source: MetadataSourceResource | null;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditSourceModal({ source, isOpen, onModalClose }: EditSourceModalProps) {
  const SourceForm =
    source != null ? (SOURCE_REGISTRY[source.implementation]?.SettingsForm ?? null) : null;

  return (
    <Modal isOpen={isOpen} size="medium" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        {SourceForm != null && source != null ? (
          <SourceForm source={source} onModalClose={onModalClose} />
        ) : null}
      </ModalContent>
    </Modal>
  );
}

export default EditSourceModal;
