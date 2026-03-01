import React, { useCallback } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import { icons } from 'Helpers/Props';
import { QueueItem } from './useQueue';
import styles from './Queue.css';

interface QueueRowProps {
  item: QueueItem;
}

function getStatusIcon(status: QueueItem['status']) {
  if (status === 'started') {
    return <Icon name={icons.REFRESH} isSpinning={true} title="Downloading" />;
  }

  return <Icon name={icons.PENDING} title="Queued" />;
}

export default function QueueRow({ item }: QueueRowProps) {
  const {
    contentId,
    contentTitle,
    thumbnailUrl,
    creatorName,
    channelName,
    status,
    message,
  } = item;

  const { mutate: cancelDownload } = useApiMutation<void, void>({
    path: `/queue/${contentId}`,
    method: 'DELETE',
  });

  const handleCancel = useCallback(() => {
    cancelDownload(undefined);
  }, [cancelDownload]);

  return (
    <TableRow>
      <TableRowCell className={styles.thumbnailCell}>
        {thumbnailUrl ? (
          <img
            className={styles.thumbnail}
            src={thumbnailUrl}
            alt={contentTitle}
          />
        ) : null}
      </TableRowCell>

      <TableRowCell className={styles.title}>{contentTitle}</TableRowCell>

      <TableRowCell>{creatorName}</TableRowCell>

      <TableRowCell>{channelName}</TableRowCell>

      <TableRowCell className={styles.status}>
        {getStatusIcon(status)}
        {message ? (
          <span className={styles.message}> {message}</span>
        ) : null}
      </TableRowCell>

      <TableRowCell className={styles.cancelCell}>
        <button
          className={styles.cancelBtn}
          onClick={handleCancel}
          title="Cancel download"
          type="button"
        >
          <Icon name={icons.REMOVE} size={14} />
        </button>
      </TableRowCell>
    </TableRow>
  );
}
