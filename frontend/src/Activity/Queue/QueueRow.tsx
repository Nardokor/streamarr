import React, { useCallback } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import { icons } from 'Helpers/Props';
import { QueueItem, QueueItemState } from './useQueue';
import styles from './Queue.css';

interface QueueRowProps {
  item: QueueItem;
}

function formatWaitTime(fromIso: string): string {
  const seconds = Math.max(
    0,
    Math.floor((Date.now() - new Date(fromIso).getTime()) / 1000)
  );

  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m`;
}

function getStateDisplay(state: QueueItemState) {
  switch (state) {
    case 'downloading':
      return {
        icon: (
          <Icon name={icons.REFRESH} isSpinning={true} title="Downloading" />
        ),
        label: 'Downloading',
      };
    case 'liveWaiting':
      return {
        icon: (
          <Icon name={icons.NETWORK} title="Live recording (holding a slot)" />
        ),
        label: 'Live (holding slot)',
      };
    case 'waitingForSlot':
      return {
        icon: (
          <Icon name={icons.WARNING} title="Waiting for a free download slot" />
        ),
        label: 'Waiting for slot',
      };
    case 'queued':
    default:
      return {
        icon: <Icon name={icons.PENDING} title="Queued" />,
        label: 'Queued',
      };
  }
}

export default function QueueRow({ item }: QueueRowProps) {
  const {
    contentId,
    contentTitle,
    thumbnailUrl,
    creatorName,
    channelName,
    message,
    queuedAt,
    startedAt,
    state,
  } = item;

  const { mutate: cancelDownload } = useApiMutation<void, void>({
    path: `/queue/${contentId}`,
    method: 'DELETE',
  });

  const handleCancel = useCallback(() => {
    cancelDownload(undefined);
  }, [cancelDownload]);

  const { icon, label } = getStateDisplay(state);
  const waitSince = startedAt ?? queuedAt;

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
        {icon}
        <span className={styles.stateLabel}> {label}</span>
        {message ? <span className={styles.message}> {message}</span> : null}
      </TableRowCell>

      <TableRowCell className={styles.waitTime}>
        {waitSince ? formatWaitTime(waitSince) : null}
      </TableRowCell>

      <TableRowCell className={styles.cancelCell}>
        <button
          className={styles.cancelBtn}
          title="Cancel download"
          type="button"
          onClick={handleCancel}
        >
          <Icon name={icons.REMOVE} size={14} />
        </button>
      </TableRowCell>
    </TableRow>
  );
}
