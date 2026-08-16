import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import QueueRow from './QueueRow';
import { useQueue, useQueueSlots } from './useQueue';
import styles from './Queue.css';

const columns: Column[] = [
  {
    name: 'thumbnail',
    label: '',
    isVisible: true,
  },
  {
    name: 'contentTitle',
    label: 'Title',
    isVisible: true,
  },
  {
    name: 'creatorName',
    label: 'Creator',
    isVisible: true,
  },
  {
    name: 'channelName',
    label: 'Channel',
    isVisible: true,
  },
  {
    name: 'status',
    label: 'Status',
    isVisible: true,
  },
  {
    name: 'waitTime',
    label: 'Wait Time',
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

function SlotsBar() {
  const { data: slots } = useQueueSlots();

  if (!slots) {
    return null;
  }

  const busy = slots.effectiveMax - slots.availableSlots;
  const liveCount = slots.liveWaitingContentIds.length;
  const activeCount = slots.activeDownloadContentIds.length;
  const staleMismatch = slots.configuredMax !== slots.effectiveMax;

  return (
    <div className={styles.slotsBar}>
      <span className={styles.slotsSummary}>
        {busy} / {slots.effectiveMax} slots busy
      </span>
      <span className={styles.slotsBreakdown}>
        {activeCount} downloading, {liveCount} live recording
        {liveCount === 1 ? '' : 's'} holding a slot
      </span>
      {staleMismatch ? (
        <span className={styles.slotsWarning}>
          Configured max is {slots.configuredMax}, but {slots.effectiveMax} is
          in effect — restart to apply the new setting
        </span>
      ) : null}
    </div>
  );
}

function Queue() {
  const { data: items, isLoading } = useQueue();

  if (isLoading) {
    return (
      <PageContent title="Queue">
        <PageContentBody>
          <LoadingIndicator />
        </PageContentBody>
      </PageContent>
    );
  }

  return (
    <PageContent title="Queue">
      <PageContentBody>
        <SlotsBar />

        {items && items.length > 0 ? (
          <Table columns={columns}>
            <TableBody>
              {items.map((item) => (
                <QueueRow key={item.commandId} item={item} />
              ))}
            </TableBody>
          </Table>
        ) : (
          <div className={styles.empty}>No downloads in queue</div>
        )}
      </PageContentBody>
    </PageContent>
  );
}

export default Queue;
