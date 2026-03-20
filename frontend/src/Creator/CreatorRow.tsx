import React, { useCallback } from 'react';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import CommandNames from 'Commands/CommandNames';
import { useCommandExecuting, useExecuteCommand } from 'Commands/useCommands';
import { icons } from 'Helpers/Props';
import Creator from 'typings/Creator';
import { formatDate, getNextLiveDate } from './creatorUtils';
import { CreatorStats, useCreatorContent, useUpdateCreator } from './useCreators';
import styles from './CreatorRow.css';

interface CreatorRowProps {
  creator: Creator;
  stats?: CreatorStats;
}

function CreatorRow({ creator, stats }: CreatorRowProps) {
  const { id, title, monitored } = creator;

  const { data: content } = useCreatorContent(id);

  const executeCommand = useExecuteCommand();
  const isRefreshing = useCommandExecuting(CommandNames.RefreshCreator, { creatorId: id });
  const isDownloading = useCommandExecuting(CommandNames.DownloadMissingContent, { creatorId: id });
  const { updateCreator, isUpdating } = useUpdateCreator(id);

  const handleMonitorToggle = useCallback(
    (value: boolean) => {
      updateCreator({ ...creator, monitored: value });
    },
    [creator, updateCreator]
  );

  const downloaded = content.filter(
    (c) => c.contentFileId > 0 || c.status === 'downloaded'
  ).length;
  const wanted = content.filter(
    (c) => c.monitored && c.status !== 'unwanted'
  ).length;

  const nextLive = getNextLiveDate(content);

  const handleRefresh = useCallback(() => {
    executeCommand({ name: CommandNames.RefreshCreator, creatorId: id });
  }, [executeCommand, id]);

  const handleDownloadMissing = useCallback(() => {
    executeCommand({ name: CommandNames.DownloadMissingContent, creatorId: id });
  }, [executeCommand, id]);

  return (
    <TableRow>
      <TableRowCell className={styles.thumbnail}>
        {creator.thumbnailUrl ? (
          <img
            className={styles.thumbnailImg}
            src={creator.thumbnailUrl}
            alt={title}
          />
        ) : (
          <div className={styles.thumbnailPlaceholder} />
        )}
      </TableRowCell>

      <TableRowCell className={styles.monitor}>
        <MonitorToggleButton
          monitored={monitored}
          isSaving={isUpdating}
          onPress={handleMonitorToggle}
        />
      </TableRowCell>

      <TableRowCell>
        <Link className={styles.title} to={`/creator/${creator.titleSlug}`}>
          {title}
        </Link>
      </TableRowCell>

      <TableRowCell className={styles.nextLive}>
        {stats?.isLiveNow
          ? <span className={styles.liveLabel}>LIVE{stats.liveCategory ? ` · ${stats.liveCategory}` : ''}</span>
          : nextLive ? formatDate(nextLive.toISOString()) : '—'}
      </TableRowCell>

      <TableRowCell className={styles.progress}>
        <div className={`${styles.progressBar} ${wanted === 0 ? styles.progressEmpty : downloaded === wanted ? styles.progressComplete : styles.progressIncomplete}`}>
          {downloaded} / {wanted}
        </div>
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
