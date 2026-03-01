import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { useQueue } from './useQueue';
import QueueRow from './QueueRow';
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
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

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
