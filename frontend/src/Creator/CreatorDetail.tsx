import React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import Content from 'typings/Content';
import { useCreator, useCreatorChannels, useCreatorContent } from './useCreators';
import styles from './CreatorDetail.css';

interface RouteParams {
  id: string;
}

const contentColumns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'airDate', label: 'Date', isVisible: true },
  { name: 'duration', label: 'Duration', isVisible: true },
  { name: 'status', label: 'Status', isVisible: true },
];

function formatDuration(duration: string | null): string {
  if (!duration) {
    return '—';
  }
  // Duration comes as ISO 8601 from .NET TimeSpan (e.g. "00:10:32")
  const parts = duration.split(':');
  if (parts.length === 3) {
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    const s = parseInt(parts[2], 10);
    if (h > 0) {
      return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }
    return `${m}:${String(s).padStart(2, '0')}`;
  }
  return duration;
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) {
    return '—';
  }
  return new Date(dateStr).toLocaleDateString();
}

function statusLabel(content: Content): { text: string; className: string } {
  if (content.contentFileId > 0) {
    return { text: 'Downloaded', className: styles.statusDownloaded };
  }
  if (content.monitored) {
    return { text: 'Missing', className: styles.statusMissing };
  }
  return { text: 'Unmonitored', className: styles.statusMonitored };
}

type Props = RouteComponentProps<RouteParams>;

function CreatorDetail({ match }: Props) {
  const creatorId = parseInt(match.params.id, 10);

  const { data: creator, isLoading: creatorLoading } = useCreator(creatorId);
  const { data: channels, isLoading: channelsLoading } =
    useCreatorChannels(creatorId);
  const { data: content, isLoading: contentLoading } =
    useCreatorContent(creatorId);

  const isLoading = creatorLoading || channelsLoading || contentLoading;

  if (isLoading) {
    return (
      <PageContent title="Creator">
        <PageContentBody>
          <LoadingIndicator />
        </PageContentBody>
      </PageContent>
    );
  }

  if (!creator) {
    return (
      <PageContent title="Creator">
        <PageContentBody>
          <Alert kind={kinds.DANGER}>Creator not found.</Alert>
        </PageContentBody>
      </PageContent>
    );
  }

  return (
    <PageContent title={creator.title}>
      <PageContentBody>
        {/* Header */}
        <div className={styles.header}>
          {creator.thumbnailUrl ? (
            <img
              className={styles.headerThumbnail}
              src={creator.thumbnailUrl}
              alt={creator.title}
            />
          ) : null}

          <div className={styles.headerInfo}>
            <div className={styles.headerTitle}>{creator.title}</div>

            <div className={styles.headerMeta}>
              <span>
                <Icon name={creator.monitored ? icons.MONITORED : icons.UNMONITORED} />
                {' '}
                {creator.monitored ? 'Monitored' : 'Unmonitored'}
              </span>
              <span>{creator.path}</span>
              {creator.lastInfoSync ? (
                <span>Last synced: {formatDate(creator.lastInfoSync)}</span>
              ) : null}
            </div>

            {creator.description ? (
              <div className={styles.headerDescription}>
                {creator.description.slice(0, 300)}
                {creator.description.length > 300 ? '…' : ''}
              </div>
            ) : null}
          </div>
        </div>

        {/* Channels */}
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            Channels ({channels.length})
          </div>

          <div className={styles.channelList}>
            {channels.map((ch) => (
              <div key={ch.id} className={styles.channelCard}>
                <span className={styles.channelBadge}>{ch.platform}</span>
                <span className={styles.channelTitle}>{ch.title}</span>
                <span className={styles.channelUrl}>{ch.platformUrl}</span>
                <Icon
                  name={ch.monitored ? icons.MONITORED : icons.UNMONITORED}
                  title={ch.monitored ? 'Monitored' : 'Unmonitored'}
                />
              </div>
            ))}
          </div>
        </div>

        {/* Content */}
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            Content ({content.length})
          </div>

          {content.length === 0 ? (
            <Alert kind={kinds.INFO}>
              No content synced yet. A sync runs every 60 minutes, or you can
              trigger one from System &rsaquo; Tasks.
            </Alert>
          ) : (
            <Table columns={contentColumns}>
              <TableBody>
                {content.map((item) => {
                  const status = statusLabel(item);
                  return (
                    <TableRow key={item.id}>
                      <TableRowCell className={styles.contentThumbnail}>
                        {item.thumbnailUrl ? (
                          <img
                            className={styles.contentThumbnailImg}
                            src={item.thumbnailUrl}
                            alt={item.title}
                          />
                        ) : null}
                      </TableRowCell>
                      <TableRowCell>{item.title}</TableRowCell>
                      <TableRowCell>{formatDate(item.airDateUtc)}</TableRowCell>
                      <TableRowCell>{formatDuration(item.duration)}</TableRowCell>
                      <TableRowCell>
                        <span className={`${styles.statusBadge} ${status.className}`}>
                          {status.text}
                        </span>
                      </TableRowCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </div>
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorDetail;
