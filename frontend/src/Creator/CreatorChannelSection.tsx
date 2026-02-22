import React, { useCallback, useEffect, useState } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import CommandNames from 'Commands/CommandNames';
import { useExecuteCommand } from 'Commands/useCommands';
import { icons } from 'Helpers/Props';
import Channel from 'typings/Channel';
import Content from 'typings/Content';
import {
  buildPlatformUrl,
  formatDate,
  formatDuration,
  getContentTypeLabel,
  getStatusLabel,
} from './creatorUtils';
import { useDeleteChannel, useUpdateChannel } from './useCreators';
import styles from './CreatorChannelSection.css';

interface CreatorChannelSectionProps {
  channel: Channel;
  content: Content[];
}

const columns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'type', label: 'Type', isVisible: true },
  { name: 'airDate', label: 'Date', isVisible: true },
  { name: 'duration', label: 'Duration', isVisible: true },
  { name: 'status', label: 'Status', isVisible: true },
  { name: 'download', label: '', isVisible: true },
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
  if (kind === 'downloading') return styles.statusDownloading;
  if (kind === 'missing') return styles.statusMissing;
  if (kind === 'notAired') return styles.statusNotAired;
  return styles.statusUnmonitored;
}

function typeClass(label: string): string {
  if (label === 'Video') return styles.typeVideo;
  if (label === 'Short') return styles.typeShort;
  if (label === 'Live') return styles.typeLive;
  return '';
}

function CreatorChannelSection({ channel, content }: CreatorChannelSectionProps) {
  const [expanded, setExpanded] = useState(true);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(false);

  const [dlVideos, setDlVideos] = useState(channel.downloadVideos);
  const [dlShorts, setDlShorts] = useState(channel.downloadShorts);
  const [dlLivestreams, setDlLivestreams] = useState(channel.downloadLivestreams);
  const [titleFilter, setTitleFilter] = useState(channel.titleFilter);

  useEffect(() => {
    setDlVideos(channel.downloadVideos);
    setDlShorts(channel.downloadShorts);
    setDlLivestreams(channel.downloadLivestreams);
    setTitleFilter(channel.titleFilter);
  }, [channel.downloadVideos, channel.downloadShorts, channel.downloadLivestreams, channel.titleFilter]);

  const { updateChannel, isUpdating } = useUpdateChannel(channel.id, channel.creatorId);
  const { deleteChannel } = useDeleteChannel(channel.id, channel.creatorId);
  const executeCommand = useExecuteCommand();

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
                {[...content]
                  .sort((a, b) => {
                    if (!a.airDateUtc && !b.airDateUtc) return 0;
                    if (!a.airDateUtc) return 1;
                    if (!b.airDateUtc) return -1;
                    return new Date(b.airDateUtc).getTime() - new Date(a.airDateUtc).getTime();
                  })
                  .map((item) => {
                    const status = getStatusLabel(item);
                    const typeLabel = getContentTypeLabel(item.contentType);
                    const videoUrl = buildPlatformUrl(channel.platform, item.platformContentId);
                    const canDownload =
                      item.monitored &&
                      status.kind !== 'downloaded' &&
                      status.kind !== 'downloading' &&
                      status.kind !== 'notAired';

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

                        <TableRowCell>
                          {videoUrl ? (
                            <a
                              className={styles.titleLink}
                              href={videoUrl}
                              target="_blank"
                              rel="noreferrer"
                            >
                              {item.title}
                            </a>
                          ) : (
                            item.title
                          )}
                        </TableRowCell>

                        <TableRowCell>
                          {typeLabel ? (
                            <span className={`${styles.typeBadge} ${typeClass(typeLabel)}`}>
                              {typeLabel}
                            </span>
                          ) : null}
                        </TableRowCell>

                        <TableRowCell>{formatDate(item.airDateUtc)}</TableRowCell>

                        <TableRowCell>{formatDuration(item.duration)}</TableRowCell>

                        <TableRowCell>
                          <span className={`${styles.statusBadge} ${statusClass(status.kind)}`}>
                            {status.text}
                          </span>
                        </TableRowCell>

                        <TableRowCell className={styles.downloadCell}>
                          {canDownload ? (
                            <IconButton
                              name={icons.DOWNLOAD}
                              size={12}
                              title="Download"
                              onPress={() =>
                                executeCommand({
                                  name: CommandNames.DownloadContent,
                                  contentId: item.id,
                                })
                              }
                            />
                          ) : null}
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
