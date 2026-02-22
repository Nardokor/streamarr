import React, { useCallback } from 'react';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import CommandNames from 'Commands/CommandNames';
import { useCommandExecuting, useExecuteCommand } from 'Commands/useCommands';
import { icons } from 'Helpers/Props';
import Creator from 'typings/Creator';
import { formatDate, getNextLiveDate } from './creatorUtils';
import { useCreatorChannels, useCreatorContent } from './useCreators';
import styles from './CreatorRow.css';

interface CreatorRowProps {
  creator: Creator;
}

function CreatorRow({ creator }: CreatorRowProps) {
  const { id, title, monitored } = creator;

  const { data: channels } = useCreatorChannels(id);
  const { data: content } = useCreatorContent(id);

  const executeCommand = useExecuteCommand();
  const isRefreshing = useCommandExecuting(CommandNames.RefreshCreator, { creatorId: id });
  const isDownloading = useCommandExecuting(CommandNames.DownloadMissingContent, { creatorId: id });

  const hasFilter = channels.some((ch) => ch.titleFilter && ch.titleFilter.trim() !== '');
  const monitorType = !monitored ? 'None' : hasFilter ? 'Filter' : 'All';

  const downloaded = content.filter(
    (c) => c.contentFileId > 0 || c.status === 'downloaded'
  ).length;
  const total = content.length;
  const progress = total > 0 ? (downloaded / total) * 100 : 0;

  const nextLive = getNextLiveDate(content);

  const handleRefresh = useCallback(() => {
    executeCommand({ name: CommandNames.RefreshCreator, creatorId: id });
  }, [executeCommand, id]);

  const handleDownloadMissing = useCallback(() => {
    executeCommand({ name: CommandNames.DownloadMissingContent, creatorId: id });
  }, [executeCommand, id]);

  const monitorClass =
    monitorType === 'All'
      ? styles.monitorAll
      : monitorType === 'Filter'
      ? styles.monitorFilter
      : styles.monitorNone;

  return (
    <TableRow>
      <TableRowCell className={styles.monitor}>
        <span className={`${styles.monitorBadge} ${monitorClass}`}>
          {monitorType}
        </span>
      </TableRowCell>

      <TableRowCell>
        <Link className={styles.title} to={`/creator/${id}`}>
          {title}
        </Link>
      </TableRowCell>

      <TableRowCell className={styles.nextLive}>
        {nextLive ? formatDate(nextLive.toISOString()) : '—'}
      </TableRowCell>

      <TableRowCell className={styles.progress}>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{ width: `${progress}%` }}
          />
        </div>
        <span className={styles.progressText}>
          {downloaded} / {total}
        </span>
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        <IconButton
          name={icons.REFRESH}
          size={12}
          title="Refresh"
          isSpinning={isRefreshing}
          onPress={handleRefresh}
        />
        <IconButton
          name={icons.DOWNLOAD}
          size={12}
          title="Download Missing"
          isSpinning={isDownloading}
          onPress={handleDownloadMissing}
        />
      </TableRowCell>
    </TableRow>
  );
}

export default CreatorRow;
