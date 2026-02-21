import React from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
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
    contentTitle,
    thumbnailUrl,
    creatorName,
    channelName,
    status,
    message,
  } = item;

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
    </TableRow>
  );
}
