import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { useHistory } from './useHistory';
import styles from './History.css';

const columns: Column[] = [
  { name: 'date', label: 'Date', isVisible: true },
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'eventType', label: 'Event', isVisible: true },
  { name: 'data', label: 'Detail', isVisible: true },
];

function eventTypeLabel(eventType: string): string {
  switch (eventType) {
    case 'Downloaded': return 'Downloaded';
    case 'DownloadFailed': return 'Failed';
    case 'Deleted': return 'Deleted';
    case 'Ignored': return 'Ignored';
    default: return eventType;
  }
}

function eventTypeClass(eventType: string): string {
  switch (eventType) {
    case 'Downloaded': return styles.eventDownloaded;
    case 'DownloadFailed': return styles.eventFailed;
    case 'Deleted': return styles.eventDeleted;
    default: return '';
  }
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);

  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function History() {
  const { data: items, isLoading } = useHistory();

  if (isLoading) {
    return (
      <PageContent title="History">
        <PageContentBody>
          <LoadingIndicator />
        </PageContentBody>
      </PageContent>
    );
  }

  return (
    <PageContent title="History">
      <PageContentBody>
        {items.length === 0 ? (
          <div className={styles.empty}>No history yet.</div>
        ) : (
          <Table columns={columns}>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableRowCell className={styles.date}>
                    {formatDate(item.date)}
                  </TableRowCell>

                  <TableRowCell>{item.title}</TableRowCell>

                  <TableRowCell>
                    <span className={`${styles.eventBadge} ${eventTypeClass(item.eventType)}`}>
                      {eventTypeLabel(item.eventType)}
                    </span>
                  </TableRowCell>

                  <TableRowCell className={styles.detail}>
                    {item.data || '—'}
                  </TableRowCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </PageContentBody>
    </PageContent>
  );
}

export default History;
