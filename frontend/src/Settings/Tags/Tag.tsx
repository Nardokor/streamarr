import React, { useCallback, useState } from 'react';
import Card from 'Components/Card';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { kinds } from 'Helpers/Props';
import { useDeleteTag } from 'Tags/useTags';
import translate from 'Utilities/String/translate';
import TagDetailsModal from './Details/TagDetailsModal';
import styles from './Tag.css';

interface TagProps {
  id: number;
  label: string;
}

function Tag({ id, label }: TagProps) {
  const { deleteTag } = useDeleteTag(id);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [isDeleteTagModalOpen, setIsDeleteTagModalOpen] = useState(false);

  const isTagUsed = false;

  const handleShowDetailsPress = useCallback(() => {
    setIsDetailsModalOpen(true);
  }, []);

  const handeDetailsModalClose = useCallback(() => {
    setIsDetailsModalOpen(false);
  }, []);

  const handleDeleteTagPress = useCallback(() => {
    setIsDetailsModalOpen(false);
    setIsDeleteTagModalOpen(true);
  }, []);

  const handleConfirmDeleteTag = useCallback(() => {
    deleteTag();
  }, [deleteTag]);

  const handleDeleteTagModalClose = useCallback(() => {
    setIsDeleteTagModalOpen(false);
  }, []);

  return (
    <Card
      className={styles.tag}
      overlayContent={true}
      onPress={handleShowDetailsPress}
    >
      <div className={styles.label}>{label}</div>

      {!isTagUsed && <div>{translate('NoLinks')}</div>}

      <TagDetailsModal
        label={label}
        isTagUsed={isTagUsed}
        isOpen={isDetailsModalOpen}
        onModalClose={handeDetailsModalClose}
        onDeleteTagPress={handleDeleteTagPress}
      />

      <ConfirmModal
        isOpen={isDeleteTagModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteTag')}
        message={translate('DeleteTagMessageText', { label })}
        confirmLabel={translate('Delete')}
        onConfirm={handleConfirmDeleteTag}
        onCancel={handleDeleteTagModalClose}
      />
    </Card>
  );
}

export default Tag;
