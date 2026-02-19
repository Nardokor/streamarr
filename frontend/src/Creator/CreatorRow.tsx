import React, { useCallback, useState } from 'react';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import Creator from 'typings/Creator';
import { useDeleteCreator } from './useCreators';
import styles from './CreatorRow.css';

interface CreatorRowProps {
  creator: Creator;
}

function CreatorRow({ creator }: CreatorRowProps) {
  const { id, title, thumbnailUrl, path, monitored } = creator;
  const { deleteCreator } = useDeleteCreator(id);

  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);

  const handleDeletePress = useCallback(() => {
    setIsConfirmDeleteOpen(true);
  }, []);

  const handleConfirmDeleteModalClose = useCallback(() => {
    setIsConfirmDeleteOpen(false);
  }, []);

  const handleConfirmDeletePress = useCallback(() => {
    deleteCreator(undefined, {
      onSuccess: () => {
        setIsConfirmDeleteOpen(false);
      },
    });
  }, [deleteCreator]);

  return (
    <TableRow>
      <TableRowCell className={styles.thumbnail}>
        {thumbnailUrl ? (
          <img
            className={styles.thumbnailImg}
            src={thumbnailUrl}
            alt={title}
          />
        ) : null}
      </TableRowCell>

      <TableRowCell>
        <span className={styles.title}>{title}</span>
      </TableRowCell>

      <TableRowCell>{path}</TableRowCell>

      <TableRowCell>
        <IconButton
          name={monitored ? icons.MONITORED : icons.UNMONITORED}
          title={monitored ? 'Monitored' : 'Unmonitored'}
        />
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        <IconButton
          title="Delete Creator"
          name={icons.DELETE}
          kind={kinds.DANGER}
          onPress={handleDeletePress}
        />
      </TableRowCell>

      <ConfirmModal
        isOpen={isConfirmDeleteOpen}
        kind={kinds.DANGER}
        title="Delete Creator"
        message={`Are you sure you want to delete '${title}'? This will remove all tracked content for this creator.`}
        confirmLabel="Delete"
        onConfirm={handleConfirmDeletePress}
        onCancel={handleConfirmDeleteModalClose}
      />
    </TableRow>
  );
}

export default CreatorRow;
