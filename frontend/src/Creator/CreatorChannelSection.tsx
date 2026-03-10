import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ASCENDING, DESCENDING, SortDirection } from 'Helpers/Props/sortDirections';
import ContentDetailModal from './ContentDetailModal';
import FormGroup from 'Components/Form/FormGroup';
import FormLabel from 'Components/Form/FormLabel';
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
  { name: 'title', label: 'Title', isVisible: true, isSortable: true },
  { name: 'type', label: 'Type', isVisible: true, isSortable: true },
  { name: 'members', label: '', isVisible: true },
  { name: 'airDate', label: 'Date', isVisible: true, isSortable: true },
  { name: 'duration', label: 'Duration', isVisible: true, isSortable: true },
  { name: 'status', label: 'Status', isVisible: true, isSortable: true },
  { name: 'download', label: '', isVisible: true },
];

const TYPE_LABELS = ['Video', 'Short', 'VoD', 'Live', 'Upcoming'] as const;
const STATUS_FILTERS: { kind: string; label: string }[] = [
  { kind: 'downloaded', label: 'Downloaded' },
  { kind: 'missing', label: 'Missing' },
  { kind: 'unwanted', label: 'Unwanted' },
  { kind: 'recording', label: 'Recording' },
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
  const [filterOpen, setFilterOpen] = useState(false);
  const [filterMembers, setFilterMembers] = useState(false);
  const [filterTypes, setFilterTypes] = useState<Set<string>>(new Set());
  const [filterStatuses, setFilterStatuses] = useState<Set<string>>(new Set());
  const [searchText, setSearchText] = useState('');
  const [sortKey, setSortKey] = useState('airDate');
  const [sortDir, setSortDir] = useState<SortDirection>(DESCENDING);

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
  const [keepVideos, setKeepVideos] = useState(channel.keepVideos);
  const [keepShorts, setKeepShorts] = useState(channel.keepShorts);
  const [keepVods, setKeepVods] = useState(channel.keepVods);
  const [keepMembers, setKeepMembers] = useState(channel.keepMembers);
  const [retentionKeepWords, setRetentionKeepWords] = useState(channel.retentionKeepWords);

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
    setKeepVideos(channel.keepVideos);
    setKeepShorts(channel.keepShorts);
    setKeepVods(channel.keepVods);
    setKeepMembers(channel.keepMembers);
    setRetentionKeepWords(channel.retentionKeepWords);
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
    channel.keepVideos,
    channel.keepShorts,
    channel.keepVods,
    channel.keepMembers,
    channel.retentionKeepWords,
  ]);

  const queryClient = useQueryClient();
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

  const handleRecheckMembership = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      // Reset to Unknown so the next sync probes the membership tab.
      updateChannel(
        { ...channel, membershipStatus: 'unknown' as const, lastMembershipCheck: null },
        {
          onSuccess: () =>
            executeCommand(
              { name: CommandNames.RefreshCreator, creatorId: channel.creatorId },
              () => queryClient.invalidateQueries({ queryKey: [`/channel/creator/${channel.creatorId}`] })
            ),
        }
      );
    },
    [channel, updateChannel, executeCommand, queryClient]
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
        keepVideos,
        keepShorts,
        keepVods,
        keepMembers,
        retentionKeepWords,
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
    keepVideos,
    keepShorts,
    keepVods,
    keepMembers,
    retentionKeepWords,
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
    setKeepVideos(channel.keepVideos);
    setKeepShorts(channel.keepShorts);
    setKeepVods(channel.keepVods);
    setKeepMembers(channel.keepMembers);
    setRetentionKeepWords(channel.retentionKeepWords);
    setSettingsOpen(false);
  }, [channel]);

  const handleSortPress = useCallback((name: string) => {
    if (name === sortKey) {
      setSortDir((d) => (d === DESCENDING ? ASCENDING : DESCENDING));
    } else {
      setSortKey(name);
      setSortDir(name === 'title' || name === 'type' || name === 'status' ? ASCENDING : DESCENDING);
    }
  }, [sortKey]);

  const toggleType = useCallback((label: string) => {
    setFilterTypes((prev) => {
      const next = new Set(prev);
      next.has(label) ? next.delete(label) : next.add(label);
      return next;
    });
  }, []);

  const toggleStatus = useCallback((kind: string) => {
    setFilterStatuses((prev) => {
      const next = new Set(prev);
      next.has(kind) ? next.delete(kind) : next.add(kind);
      return next;
    });
  }, []);

  const platform = platformLabel(channel.platform);

  const displayContent = useMemo(() => {
    let items = [...content];

    if (searchText.trim()) {
      const s = searchText.toLowerCase();
      items = items.filter((i) => i.title.toLowerCase().includes(s));
    }
    if (filterTypes.size > 0) {
      items = items.filter((i) => filterTypes.has(getContentTypeLabel(i.contentType) ?? ''));
    }
    if (filterStatuses.size > 0) {
      items = items.filter((i) => filterStatuses.has(getStatusLabel(i).kind));
    }
    if (filterMembers) {
      items = items.filter((i) => i.isMembers);
    }

    items.sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case 'airDate':
          if (!a.airDateUtc && !b.airDateUtc) cmp = 0;
          else if (!a.airDateUtc) cmp = 1;
          else if (!b.airDateUtc) cmp = -1;
          else cmp = new Date(a.airDateUtc).getTime() - new Date(b.airDateUtc).getTime();
          break;
        case 'title':
          cmp = a.title.localeCompare(b.title);
          break;
        case 'type':
          cmp = (getContentTypeLabel(a.contentType) ?? '').localeCompare(getContentTypeLabel(b.contentType) ?? '');
          break;
        case 'duration': {
          const toSec = (d: string | null) => {
            if (!d) return 0;
            const parts = d.split(':').map(Number);
            return (parts[0] ?? 0) * 3600 + (parts[1] ?? 0) * 60 + (parts[2] ?? 0);
          };
          cmp = toSec(a.duration) - toSec(b.duration);
          break;
        }
        case 'status':
          cmp = getStatusLabel(a).kind.localeCompare(getStatusLabel(b).kind);
          break;
        default:
          cmp = 0;
      }
      return sortDir === DESCENDING ? -cmp : cmp;
    });

    return items;
  }, [content, searchText, filterTypes, filterStatuses, filterMembers, sortKey, sortDir]);

  return (
    <div className={styles.section}>
      <div className={styles.header} onClick={handleToggle}>
        <span className={`${styles.chevron} ${expanded ? '' : styles.chevronCollapsed}`}>▼</span>
        <span className={styles.platformBadge}>{platform}</span>
        <span className={styles.channelTitle}>{channel.title}</span>

        <span className={styles.headerActions} onClick={(e) => e.stopPropagation()}>
          <button
            className={`${styles.iconBtn} ${
              channel.membershipStatus === 'active' ? styles.iconBtnMembershipActive :
              channel.membershipStatus === 'none' ? styles.iconBtnMembershipNone :
              ''
            }`}
            onClick={handleRecheckMembership}
            title={
              channel.membershipStatus === 'active'
                ? `Membership active — last checked ${channel.lastMembershipCheck ? formatDate(channel.lastMembershipCheck) : 'never'}. Click to re-check.`
                : channel.membershipStatus === 'none'
                ? `No membership — last checked ${channel.lastMembershipCheck ? formatDate(channel.lastMembershipCheck) : 'never'}. Click to re-check.`
                : 'Membership status unknown — click to check'
            }
            type="button"
            disabled={isRefreshing}
          >
            <Icon name={icons.LOCK} size={12} />
          </button>

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
            className={`${styles.iconBtn} ${(filterOpen || filterTypes.size > 0 || filterStatuses.size > 0 || filterMembers || searchText) ? styles.iconBtnActive : ''}`}
            onClick={(e) => { e.stopPropagation(); setFilterOpen((p) => !p); }}
            title="Filter content"
            type="button"
          >
            <Icon name={icons.FILTER} size={12} />
          </button>

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

            <FormGroup>
              <FormLabel size="small">Content types</FormLabel>
              <div className={styles.checkboxGroup}>
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
            </FormGroup>

            <FormGroup>
              <FormLabel size="small">Watched words</FormLabel>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … (blank = all)"
                value={watchedWords}
                onChange={(e) => setWatchedWords(e.target.value)}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel size="small">Ignored words</FormLabel>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … (blank = none)"
                value={ignoredWords}
                onChange={(e) => setIgnoredWords(e.target.value)}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel size="small">{' '}</FormLabel>
              <label className={styles.checkLabel}>
                <input
                  type="checkbox"
                  checked={watchedDefeatsIgnored}
                  onChange={(e) => setWatchedDefeatsIgnored(e.target.checked)}
                />
                {' '}Watched words take priority over ignored words
              </label>
            </FormGroup>
          </div>

          {/* Download */}
          <div className={styles.settingsSection}>
            <div className={styles.settingsSectionTitle}>Download</div>

            <FormGroup>
              <FormLabel size="small">Mode</FormLabel>
              <div className={styles.checkboxGroup}>
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
            </FormGroup>
          </div>

          {/* Retention */}
          <div className={styles.settingsSection}>
            <div className={styles.settingsSectionTitle}>Retention</div>

            <FormGroup>
              <FormLabel size="small">Retention days</FormLabel>
              <input
                className={styles.filterInput}
                type="number"
                min="0"
                placeholder="days (blank = global default)"
                value={retentionDays}
                onChange={(e) => setRetentionDays(e.target.value)}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel size="small">Always keep</FormLabel>
              <div className={styles.checkboxGroup}>
                {hasField('defaultKeepVideos') && (
                  <label className={styles.checkLabel}>
                    <input type="checkbox" checked={keepVideos} onChange={(e) => setKeepVideos(e.target.checked)} />
                    {' '}Videos
                  </label>
                )}
                {hasField('defaultKeepShorts') && (
                  <label className={styles.checkLabel}>
                    <input type="checkbox" checked={keepShorts} onChange={(e) => setKeepShorts(e.target.checked)} />
                    {' '}Shorts
                  </label>
                )}
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={keepVods} onChange={(e) => setKeepVods(e.target.checked)} />
                  {' '}VoDs
                </label>
                <label className={styles.checkLabel}>
                  <input type="checkbox" checked={keepMembers} onChange={(e) => setKeepMembers(e.target.checked)} />
                  {' '}Members
                </label>
              </div>
            </FormGroup>

            <FormGroup>
              <FormLabel size="small">Keep words</FormLabel>
              <input
                className={styles.filterInput}
                type="text"
                placeholder="word1, word2 … — matching titles are never deleted"
                value={retentionKeepWords}
                onChange={(e) => setRetentionKeepWords(e.target.value)}
              />
            </FormGroup>
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
          {filterOpen ? (
          <div className={styles.filterBar} onClick={(e) => e.stopPropagation()}>
            <input
              className={styles.searchInput}
              type="text"
              placeholder="Search titles…"
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />

            <div className={styles.filterChips}>
              {TYPE_LABELS.map((label) => (
                <button
                  key={label}
                  type="button"
                  className={`${styles.filterChip} ${filterTypes.has(label) ? styles.filterChipActive : ''}`}
                  onClick={() => toggleType(label)}
                >
                  {label}
                </button>
              ))}
            </div>

            <div className={styles.filterChips}>
              {STATUS_FILTERS.map(({ kind, label }) => (
                <button
                  key={kind}
                  type="button"
                  className={`${styles.filterChip} ${filterStatuses.has(kind) ? styles.filterChipActive : ''}`}
                  onClick={() => toggleStatus(kind)}
                >
                  {label}
                </button>
              ))}
              <button
                type="button"
                className={`${styles.filterChip} ${filterMembers ? styles.filterChipActive : ''}`}
                onClick={() => setFilterMembers((p) => !p)}
              >
                <Icon name={icons.LOCK_OPEN} size={10} />
                {' '}Members
              </button>
            </div>
          </div>
          ) : null}

          {displayContent.length === 0 ? (
            <div className={styles.emptyNote}>
              {(filterTypes.size > 0 || filterStatuses.size > 0 || filterMembers || searchText)
                ? 'No content matches the current filters.'
                : 'No content synced yet for this channel.'}
            </div>
          ) : (
            <Table
              columns={columns}
              sortKey={sortKey}
              sortDirection={sortDir}
              onSortPress={handleSortPress}
            >
              <TableBody>
                {displayContent.map((item) => {
                    const status = getStatusLabel(item);
                    const downloadPercent = downloadProgress.get(item.id);
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

                        <TableRowCell onClick={(e) => e.stopPropagation()}>
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
                          <span
                            className={`${styles.statusBadge} ${statusClass(status.kind)}`}
                            style={
                              status.kind === 'downloading' && downloadPercent != null
                                ? ({ '--download-pct': `${downloadPercent}%` } as React.CSSProperties)
                                : undefined
                            }
                          >
                            {status.text}
                          </span>
                        </TableRowCell>

                        <TableRowCell className={styles.downloadCell} onClick={(e) => e.stopPropagation()}>
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
