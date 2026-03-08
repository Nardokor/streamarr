import React, { useCallback, useEffect, useState } from 'react';
import ContentDetailModal from './ContentDetailModal';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import CommandNames from 'Commands/CommandNames';
import { useCommandExecuting, useExecuteCommand } from 'Commands/useCommands';
import useDownloadProgress from 'Commands/useDownloadProgress';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
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
import {
  useMetadataSources,
  MetadataSourceResource,
} from 'Settings/Sources/useMetadataSources';
import styles from './CreatorChannelSection.css';

// Maps the camelCase platform value on Channel to the implementation name on
// MetadataSourceResource so we can look up the source's field schema.
const PLATFORM_IMPLEMENTATION: Record<string, string> = {
  youTube: 'YouTube',
  twitch: 'Twitch',
};

function getSourceFields(
  sources: MetadataSourceResource[] | undefined,
  platform: string
): Set<string> {
  const impl = PLATFORM_IMPLEMENTATION[platform];
  const source = (sources ?? []).find((s) => s.implementation === impl && s.enable);
  return new Set((source?.fields ?? []).map((f) => f.name));
}

interface CreatorChannelSectionProps {
  channel: Channel;
  content: Content[];
}

const columns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'type', label: 'Type', isVisible: true },
  { name: 'members', label: '', isVisible: true },
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
  if (kind === 'queued') return styles.statusQueued;
  if (kind === 'recording') return styles.statusRecording;
  if (kind === 'processing') return styles.statusDownloading;
  if (kind === 'missing') return styles.statusMissing;
  if (kind === 'notAired') return styles.statusNotAired;
  if (kind === 'expired') return styles.statusExpired;
  if (kind === 'modified') return styles.statusModified;
  if (kind === 'unwanted') return styles.statusUnmonitored;
  if (kind === 'available') return styles.statusAvailable;
  if (kind === 'unavailable') return styles.statusUnmonitored;
  return styles.statusUnmonitored;
}

function typeClass(label: string): string {
  if (label === 'Video') return styles.typeVideo;
  if (label === 'Short') return styles.typeShort;
  if (label === 'VoD') return styles.typeVod;
  if (label === 'Live') return styles.typeLive;
  if (label === 'Upcoming') return styles.typeUpcoming;
  return '';
}

interface DownloadCellProps {
  contentId: number;
  statusKind: string;
  monitored: boolean;
  onDownload: () => void;
}

function DownloadCell({ contentId, statusKind, monitored, onDownload }: DownloadCellProps) {
  const { mutate: cancelDownload } = useApiMutation<void, void>({
    path: `/queue/${contentId}`,
    method: 'DELETE',
  });

  const isActive = statusKind === 'downloading' || statusKind === 'recording' || statusKind === 'queued';

  if (isActive) {
    return (
      <IconButton
        name={icons.REMOVE}
        size={12}
        title="Cancel download"
        onPress={() => cancelDownload(undefined)}
      />
    );
  }

  const canDownload =
    monitored &&
    statusKind !== 'downloaded' &&
    statusKind !== 'notAired' &&
    statusKind !== 'unavailable';

  if (canDownload) {
    return (
      <IconButton
        name={icons.DOWNLOAD}
        size={12}
        title="Download"
        onPress={onDownload}
      />
    );
  }

  return null;
}

function CreatorChannelSection({ channel, content }: CreatorChannelSectionProps) {
  const { data: sources } = useMetadataSources();
  const sourceFields = getSourceFields(sources, channel.platform);
  const hasField = (name: string) => sourceFields.has(name);

  const [expanded, setExpanded] = useState(true);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState(false);
  const [selectedContentId, setSelectedContentId] = useState<number | null>(null);
  const [filterMembers, setFilterMembers] = useState(false);

  // Wanted — content types
  const [dlVideos, setDlVideos] = useState(channel.downloadVideos);
  const [dlShorts, setDlShorts] = useState(channel.downloadShorts);
  const [dlVods, setDlVods] = useState(channel.downloadVods);
  const [dlLive, setDlLive] = useState(channel.downloadLive);
  const [dlMembers, setDlMembers] = useState(channel.downloadMembers);

  // Wanted — word filters
  const [watchedWords, setWatchedWords] = useState(channel.watchedWords);
  const [ignoredWords, setIgnoredWords] = useState(channel.ignoredWords);
  const [watchedDefeatsIgnored, setWatchedDefeatsIgnored] = useState(channel.watchedDefeatsIgnored);

  // Download mode
  const [autoDownload, setAutoDownload] = useState(channel.autoDownload);

  // Retention
  const [retentionDays, setRetentionDays] = useState<string>(
    channel.retentionDays != null ? String(channel.retentionDays) : ''
  );
  const [retentionVideos, setRetentionVideos] = useState(channel.retentionVideos);
  const [retentionShorts, setRetentionShorts] = useState(channel.retentionShorts);
  const [retentionVods, setRetentionVods] = useState(channel.retentionVods);
  const [retentionLive, setRetentionLive] = useState(channel.retentionLive);
  const [retentionExceptionWords, setRetentionExceptionWords] = useState(channel.retentionExceptionWords);

  useEffect(() => {
    setDlVideos(channel.downloadVideos);
    setDlShorts(channel.downloadShorts);
    setDlVods(channel.downloadVods);
    setDlLive(channel.downloadLive);
    setDlMembers(channel.downloadMembers);
    setWatchedWords(channel.watchedWords);
    setIgnoredWords(channel.ignoredWords);
    setWatchedDefeatsIgnored(channel.watchedDefeatsIgnored);
    setAutoDownload(channel.autoDownload);
    setRetentionDays(channel.retentionDays != null ? String(channel.retentionDays) : '');
    setRetentionVideos(channel.retentionVideos);
    setRetentionShorts(channel.retentionShorts);
    setRetentionVods(channel.retentionVods);
    setRetentionLive(channel.retentionLive);
    setRetentionExceptionWords(channel.retentionExceptionWords);
  }, [
    channel.downloadVideos,
    channel.downloadShorts,
    channel.downloadVods,
    channel.downloadLive,
    channel.downloadMembers,
    channel.watchedWords,
    channel.ignoredWords,
    channel.watchedDefeatsIgnored,
    channel.autoDownload,
    channel.retentionDays,
    channel.retentionVideos,
    channel.retentionShorts,
    channel.retentionVods,
    channel.retentionLive,
    channel.retentionExceptionWords,
  ]);

  const downloadProgress = useDownloadProgress();
  const { updateChannel, isUpdating } = useUpdateChannel(channel.id, channel.creatorId);
  const { deleteChannel } = useDeleteChannel(channel.id, channel.creatorId);
  const executeCommand = useExecuteCommand();
  const isRefreshing = useCommandExecuting(CommandNames.RefreshCreator, { creatorId: channel.creatorId });

  const handleRefreshChannel = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      executeCommand({ name: CommandNames.RefreshCreator, creatorId: channel.creatorId });
    },
    [executeCommand, channel.creatorId]
  );

  const handleDownloadMissing = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      executeCommand({ name: CommandNames.DownloadMissingContent, channelId: channel.id });
    },
    [executeCommand, channel.id]
  );

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
    const parsedRetention = retentionDays.trim() === '' ? null : parseInt(retentionDays, 10);
    updateChannel(
      {
        ...channel,
        downloadVideos: dlVideos,
        downloadShorts: dlShorts,
        downloadVods: dlVods,
        downloadLive: dlLive,
        downloadMembers: dlMembers,
        watchedWords,
        ignoredWords,
        watchedDefeatsIgnored,
        autoDownload,
        retentionDays: Number.isNaN(parsedRetention as number) ? null : parsedRetention,
        retentionVideos,
        retentionShorts,
        retentionVods,
        retentionLive,
        retentionExceptionWords,
      },
      {
        onSuccess: () => setSettingsOpen(false),
      }
    );
  }, [
    channel,
    dlVideos,
    dlShorts,
    dlVods,
    dlLive,
    dlMembers,
    watchedWords,
    ignoredWords,
    watchedDefeatsIgnored,
    autoDownload,
    retentionDays,
    retentionVideos,
    retentionShorts,
    retentionVods,
    retentionLive,
    retentionExceptionWords,
    updateChannel,
  ]);

  const handleCancelSettings = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setDlVideos(channel.downloadVideos);
    setDlShorts(channel.downloadShorts);
    setDlVods(channel.downloadVods);
    setDlLive(channel.downloadLive);
    setDlMembers(channel.downloadMembers);
    setWatchedWords(channel.watchedWords);
    setIgnoredWords(channel.ignoredWords);
    setWatchedDefeatsIgnored(channel.watchedDefeatsIgnored);
    setAutoDownload(channel.autoDownload);
    setRetentionDays(channel.retentionDays != null ? String(channel.retentionDays) : '');
    setRetentionVideos(channel.retentionVideos);
    setRetentionShorts(channel.retentionShorts);
    setRetentionVods(channel.retentionVods);
    setRetentionLive(channel.retentionLive);
    setRetentionExceptionWords(channel.retentionExceptionWords);
    setSettingsOpen(false);
  }, [channel]);

  const platform = platformLabel(channel.platform);
  const displayContent = filterMembers ? content.filter((item) => item.isMembers) : content;

  return (
    <div className={styles.section}>
      <div className={styles.header} onClick={handleToggle}>
        <span className={`${styles.chevron} ${expanded ? '' : styles.chevronCollapsed}`}>▼</span>
        <span className={styles.platformBadge}>{platform}</span>
        <span className={styles.channelTitle}>{channel.title}</span>

        <span className={styles.headerActions} onClick={(e) => e.stopPropagation()}>
          <button
            className={styles.iconBtn}
            onClick={handleRefreshChannel}
            title="Refresh channel"
            type="button"
            disabled={isRefreshing}
          >
            <Icon name={icons.REFRESH} size={12} />
          </button>

          <button
            className={styles.iconBtn}
            onClick={handleDownloadMissing}
            title="Download missing"
            type="button"
          >
            <Icon name={icons.DOWNLOAD} size={12} />
          </button>

          <button
            className={`${styles.iconBtn} ${filterMembers ? styles.iconBtnActive : ''}`}
            onClick={(e) => { e.stopPropagation(); setFilterMembers((p) => !p); }}
            title={filterMembers ? 'Show all content' : 'Show members content only'}
            type="button"
          >
            <Icon name={icons.LOCK} size={12} />
          </button>

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

          {/* Wanted */}
          <div className={styles.settingsSection}>
            <div className={styles.settingsSectionTitle}>Wanted</div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Content types:</span>
              {hasField('defaultDownloadVideos') && (
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={dlVideos} onChange={(e) => setDlVideos(e.target.checked)} />
                  {' '}Videos
                </label>
              )}
              {hasField('defaultDownloadShorts') && (
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={dlShorts} onChange={(e) => setDlShorts(e.target.checked)} />
                  {' '}Shorts
                </label>
              )}
              <label className={styles.checkLabel}>
                <input type="checkbox" checked={dlVods} onChange={(e) => setDlVods(e.target.checked)} />
                {' '}VoDs
              </label>
              <label className={styles.checkLabel}>
                <input type="checkbox" checked={dlLive} onChange={(e) => setDlLive(e.target.checked)} />
                {' '}Live
              </label>
              <label className={styles.checkLabel}>
                <input type="checkbox" checked={dlMembers} onChange={(e) => setDlMembers(e.target.checked)} />
                {' '}Members
              </label>
            </div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Watched words:</span>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … (blank = all)"
                value={watchedWords}
                onChange={(e) => setWatchedWords(e.target.value)}
              />
            </div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Ignored words:</span>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … (blank = none)"
                value={ignoredWords}
                onChange={(e) => setIgnoredWords(e.target.value)}
              />
            </div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>{' '}</span>
              <label className={styles.checkLabel}>
                <input
                  type="checkbox"
                  checked={watchedDefeatsIgnored}
                  onChange={(e) => setWatchedDefeatsIgnored(e.target.checked)}
                />
                {' '}Watched words take priority over ignored words
              </label>
            </div>
          </div>

          {/* Download */}
          <div className={styles.settingsSection}>
            <div className={styles.settingsSectionTitle}>Download</div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Mode:</span>
              <label className={styles.checkLabel}>
                <input
                  type="radio"
                  name={`autoDownload-${channel.id}`}
                  checked={autoDownload}
                  onChange={() => setAutoDownload(true)}
                />
                {' '}Auto
              </label>
              <label className={styles.checkLabel}>
                <input
                  type="radio"
                  name={`autoDownload-${channel.id}`}
                  checked={!autoDownload}
                  onChange={() => setAutoDownload(false)}
                />
                {' '}Manual
              </label>
            </div>
          </div>

          {/* Retention */}
          <div className={styles.settingsSection}>
            <div className={styles.settingsSectionTitle}>Retention</div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Retention days:</span>
              <input
                className={styles.filterInput}
                type="number"
                min="0"
                placeholder="days (blank = global default)"
                value={retentionDays}
                onChange={(e) => setRetentionDays(e.target.value)}
              />
            </div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Apply to:</span>
              {hasField('defaultRetentionVideos') && (
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={retentionVideos} onChange={(e) => setRetentionVideos(e.target.checked)} />
                  {' '}Videos
                </label>
              )}
              {hasField('defaultRetentionShorts') && (
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={retentionShorts} onChange={(e) => setRetentionShorts(e.target.checked)} />
                  {' '}Shorts
                </label>
              )}
              <label className={styles.checkLabel}>
                <input type="checkbox" checked={retentionVods} onChange={(e) => setRetentionVods(e.target.checked)} />
                {' '}VoDs
              </label>
              <label className={styles.checkLabel}>
                <input type="checkbox" checked={retentionLive} onChange={(e) => setRetentionLive(e.target.checked)} />
                {' '}Live
              </label>
            </div>

            <div className={styles.settingsRow}>
              <span className={styles.settingsLabel}>Exception words:</span>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … — matching titles are never deleted"
                value={retentionExceptionWords}
                onChange={(e) => setRetentionExceptionWords(e.target.value)}
              />
            </div>
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
          {displayContent.length === 0 ? (
            <div className={styles.emptyNote}>
              {filterMembers ? 'No members content in this channel.' : 'No content synced yet for this channel.'}
            </div>
          ) : (
            <Table columns={columns}>
              <TableBody>
                {[...displayContent]
                  .sort((a, b) => {
                    if (!a.airDateUtc && !b.airDateUtc) return 0;
                    if (!a.airDateUtc) return 1;
                    if (!b.airDateUtc) return -1;
                    return new Date(b.airDateUtc).getTime() - new Date(a.airDateUtc).getTime();
                  })
                  .map((item) => {
                    const status = getStatusLabel(item, downloadProgress.get(item.id));
                    const typeLabel = getContentTypeLabel(item.contentType);
                    const videoUrl = buildPlatformUrl(channel.platform, item.platformContentId);

                    return (
                      <TableRow key={item.id} onClick={() => setSelectedContentId(item.id)} style={{ cursor: 'pointer' }}>
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

                        <TableRowCell className={styles.membersCell}>
                          {item.isMembers ? (
                            <Icon
                              name={item.isAccessible ? icons.LOCK_OPEN : icons.LOCK}
                              className={item.isAccessible ? styles.membersAccessible : styles.membersLocked}
                              size={12}
                              title={item.isAccessible ? 'Members (accessible)' : 'Members (inaccessible)'}
                            />
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
                          <DownloadCell
                            contentId={item.id}
                            statusKind={status.kind}
                            monitored={item.monitored}
                            onDownload={() =>
                              executeCommand({
                                name: CommandNames.DownloadContent,
                                contentId: item.id,
                              })
                            }
                          />
                        </TableRowCell>
                      </TableRow>
                    );
                  })}
              </TableBody>
            </Table>
          )}
        </div>
      ) : null}

      <ContentDetailModal
        contentId={selectedContentId}
        channelPlatform={channel.platform}
        onClose={() => setSelectedContentId(null)}
      />
    </div>
  );
}

export default CreatorChannelSection;
