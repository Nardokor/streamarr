import React, { useCallback, useEffect, useState } from 'react';
import Icon from 'Components/Icon';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import Channel from 'typings/Channel';
import Content from 'typings/Content';
import { formatDate, formatDuration, getStatusLabel } from './creatorUtils';
import { useDeleteChannel, useUpdateChannel } from './useCreators';
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
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(false);

  // Local settings state mirrors channel props
  const [dlVideos, setDlVideos] = useState(channel.downloadVideos);
  const [dlShorts, setDlShorts] = useState(channel.downloadShorts);
  const [dlLivestreams, setDlLivestreams] = useState(channel.downloadLivestreams);
  const [titleFilter, setTitleFilter] = useState(channel.titleFilter);

  // Keep local state in sync if channel prop changes (e.g. after save)
  useEffect(() => {
    setDlVideos(channel.downloadVideos);
    setDlShorts(channel.downloadShorts);
    setDlLivestreams(channel.downloadLivestreams);
    setTitleFilter(channel.titleFilter);
  }, [channel.downloadVideos, channel.downloadShorts, channel.downloadLivestreams, channel.titleFilter]);

  const { updateChannel, isUpdating } = useUpdateChannel(channel.id, channel.creatorId);
  const { deleteChannel } = useDeleteChannel(channel.id, channel.creatorId);

  const handleToggle = useCallback(() => {
    setExpanded((prev) => !prev);
  }, []);

  const handleSettingsToggle = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setSettingsOpen((prev) => !prev);
    setDeleteConfirm(false);
  }, []);

  const handleDeleteClick = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setDeleteConfirm(true);
    setSettingsOpen(false);
  }, []);

  const handleDeleteConfirm = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    deleteChannel();
  }, [deleteChannel]);

  const handleDeleteCancel = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setDeleteConfirm(false);
  }, []);

  const handleSaveSettings = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    updateChannel(
      {
        ...channel,
        downloadVideos: dlVideos,
        downloadShorts: dlShorts,
        downloadLivestreams: dlLivestreams,
        titleFilter,
      },
      {
        onSuccess: () => setSettingsOpen(false),
      }
    );
  }, [channel, dlVideos, dlShorts, dlLivestreams, titleFilter, updateChannel]);

  const handleCancelSettings = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setDlVideos(channel.downloadVideos);
    setDlShorts(channel.downloadShorts);
    setDlLivestreams(channel.downloadLivestreams);
    setTitleFilter(channel.titleFilter);
    setSettingsOpen(false);
  }, [channel]);

  const platform = platformLabel(channel.platform);

  return (
    <div className={styles.section}>
      <div className={styles.header} onClick={handleToggle}>
        <span className={`${styles.chevron} ${expanded ? '' : styles.chevronCollapsed}`}>▼</span>
        <span className={styles.platformBadge}>{platform}</span>
        <span className={styles.channelTitle}>{channel.title}</span>

        <span className={styles.headerActions} onClick={(e) => e.stopPropagation()}>
          {deleteConfirm ? (
            <span className={styles.deleteConfirm}>
              <span className={styles.deletePrompt}>Remove channel?</span>
              <button className={styles.confirmBtn} onClick={handleDeleteConfirm} type="button">
                Yes
              </button>
              <button className={styles.cancelBtn} onClick={handleDeleteCancel} type="button">
                No
              </button>
            </span>
          ) : (
            <button
              className={`${styles.iconBtn} ${styles.deleteBtn}`}
              onClick={handleDeleteClick}
              title="Remove channel"
              type="button"
            >
              <Icon name={icons.DELETE} size={12} />
            </button>
          )}

          <button
            className={`${styles.iconBtn} ${settingsOpen ? styles.iconBtnActive : ''}`}
            onClick={handleSettingsToggle}
            title="Channel settings"
            type="button"
          >
            <Icon name={icons.SETTINGS} size={12} />
          </button>
        </span>

        <span className={styles.count}>
          {content.length} item{content.length !== 1 ? 's' : ''}
        </span>
      </div>

      {settingsOpen ? (
        <div className={styles.settings} onClick={(e) => e.stopPropagation()}>
          <div className={styles.settingsRow}>
            <span className={styles.settingsLabel}>Download:</span>

            <label className={styles.checkLabel}>
              <input
                type="checkbox"
                checked={dlVideos}
                onChange={(e) => setDlVideos(e.target.checked)}
              />
              {' '}Videos
            </label>

            <label className={styles.checkLabel}>
              <input
                type="checkbox"
                checked={dlShorts}
                onChange={(e) => setDlShorts(e.target.checked)}
              />
              {' '}Shorts
            </label>

            <label className={styles.checkLabel}>
              <input
                type="checkbox"
                checked={dlLivestreams}
                onChange={(e) => setDlLivestreams(e.target.checked)}
              />
              {' '}Live streams
            </label>
          </div>

          <div className={styles.settingsRow}>
            <span className={styles.settingsLabel}>Title filter:</span>
            <input
              className={styles.filterInput}
              type="text"
              placeholder="keyword1, keyword2 (leave empty for all)"
              value={titleFilter}
              onChange={(e) => setTitleFilter(e.target.value)}
            />
          </div>

          <div className={styles.settingsActions}>
            <button
              className={styles.saveBtn}
              type="button"
              disabled={isUpdating}
              onClick={handleSaveSettings}
            >
              {isUpdating ? 'Saving…' : 'Save'}
            </button>

            <button
              className={styles.cancelBtn}
              type="button"
              onClick={handleCancelSettings}
            >
              Cancel
            </button>
          </div>
        </div>
      ) : null}

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
