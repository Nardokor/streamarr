import React, { useCallback, useState } from 'react';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Channel from 'typings/Channel';
import Content from 'typings/Content';
import { formatDate, formatDuration, getStatusLabel } from './creatorUtils';
import styles from './CreatorChannelSection.css';

interface CreatorChannelSectionProps {
  channel: Channel;
  content: Content[];
}

const columns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'airDate', label: 'Date', isVisible: true },
  { name: 'duration', label: 'Duration', isVisible: true },
  { name: 'status', label: 'Status', isVisible: true },
];

function platformLabel(platform: string): string {
  // platform comes as a camelCase enum string from the API (e.g. "youTube")
  const map: Record<string, string> = {
    youTube: 'YouTube',
    twitch: 'Twitch',
  };
  return map[platform] ?? platform;
}

function statusClass(kind: string): string {
  if (kind === 'downloaded') return styles.statusDownloaded;
  if (kind === 'missing') return styles.statusMissing;
  return styles.statusUnmonitored;
}

function CreatorChannelSection({ channel, content }: CreatorChannelSectionProps) {
  const [expanded, setExpanded] = useState(true);

  const handleToggle = useCallback(() => {
    setExpanded((prev) => !prev);
  }, []);

  const platform = platformLabel(channel.platform);

  return (
    <div className={styles.section}>
      <div className={styles.header} onClick={handleToggle}>
        <span className={`${styles.chevron} ${expanded ? '' : styles.chevronCollapsed}`}>▼</span>
        <span className={styles.platformBadge}>{platform}</span>
        <span className={styles.channelTitle}>{channel.title}</span>
        <span className={styles.count}>
          {content.length} item{content.length !== 1 ? 's' : ''}
        </span>
      </div>

      {expanded ? (
        <div className={styles.body}>
          {content.length === 0 ? (
            <div className={styles.emptyNote}>
              No content synced yet for this channel.
            </div>
          ) : (
            <Table columns={columns}>
              <TableBody>
                {content.map((item) => {
                  const status = getStatusLabel(item);
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
                        <span className={`${styles.statusBadge} ${statusClass(status.kind)}`}>
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
      ) : null}
    </div>
  );
}

export default CreatorChannelSection;
