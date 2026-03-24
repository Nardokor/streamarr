import React, { useCallback, useMemo, useState } from 'react';
import { useHistory } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import CommandNames from 'Commands/CommandNames';
import { useExecuteCommand } from 'Commands/useCommands';
import { useAllChannels } from 'Creator/useCreators';
import useCreators from 'Creator/useCreators';
import { icons, kinds } from 'Helpers/Props';
import Channel from 'typings/Channel';
import fetchJson from 'Utilities/Fetch/fetchJson';
import getQueryPath from 'Utilities/Fetch/getQueryPath';
import BulkEditChannelModal from './BulkEditChannelModal';
import styles from './ChannelBulkSettings.css';

const PLATFORM_LABELS: Record<string, string> = {
  youTube: 'YouTube',
  twitch: 'Twitch',
  fourthwall: 'Fourthwall',
};

function ChannelBulkSettings() {
  const history = useHistory();
  const { data: channels, isLoading, error } = useAllChannels();
  const { data: creators } = useCreators();
  const queryClient = useQueryClient();
  const executeCommand = useExecuteCommand();

  const creatorsById = useMemo(
    () => new Map(creators.map((c) => [c.id, c])),
    [creators]
  );

  const [selectMode, setSelectMode] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [modalOpen, setModalOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [filterPlatform, setFilterPlatform] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortKey, setSortKey] = useState('creator');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

  const handleSortPress = useCallback(
    (key: string) => {
      if (key === sortKey) {
        setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
      } else {
        setSortKey(key);
        setSortDir('asc');
      }
    },
    [sortKey]
  );

  const displayed = useMemo(() => {
    let list = channels;
    if (filterPlatform) {
      list = list.filter((c) => c.platform === filterPlatform);
    }
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      list = list.filter(
        (c) =>
          c.title.toLowerCase().includes(q) ||
          (creatorsById.get(c.creatorId)?.title ?? '').toLowerCase().includes(q)
      );
    }
    list = [...list].sort((a, b) => {
      let cmp = 0;
      if (sortKey === 'creator') {
        cmp = (creatorsById.get(a.creatorId)?.title ?? '').localeCompare(
          creatorsById.get(b.creatorId)?.title ?? ''
        );
      } else if (sortKey === 'channel') {
        cmp = a.title.localeCompare(b.title);
      } else if (sortKey === 'platform') {
        cmp = a.platform.localeCompare(b.platform);
      } else if (sortKey === 'monitored') {
        cmp = (b.monitored ? 1 : 0) - (a.monitored ? 1 : 0);
      } else if (sortKey === 'autoDl') {
        cmp = (b.autoDownload ? 1 : 0) - (a.autoDownload ? 1 : 0);
      } else if (sortKey === 'retention') {
        cmp = (a.retentionDays ?? Infinity) - (b.retentionDays ?? Infinity);
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return list;
  }, [channels, filterPlatform, searchQuery, sortKey, sortDir, creatorsById]);

  const allSelected =
    displayed.length > 0 && displayed.every((c) => selectedIds.has(c.id));

  const handleSelectAll = useCallback(() => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (allSelected) {
        displayed.forEach((c) => next.delete(c.id));
      } else {
        displayed.forEach((c) => next.add(c.id));
      }
      return next;
    });
  }, [allSelected, displayed]);

  const handleToggle = useCallback((id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }, []);

  const handleExitSelectMode = useCallback(() => {
    setSelectMode(false);
    setSelectedIds(new Set());
  }, []);

  const handleRowClick = useCallback(
    (ch: Channel) => {
      if (selectMode) {
        handleToggle(ch.id);
        return;
      }
      const creator = creatorsById.get(ch.creatorId);
      if (creator) {
        history.push(`/creator/${creator.titleSlug}`);
      }
    },
    [selectMode, handleToggle, creatorsById, history]
  );

  const selectedChannels = useMemo(
    () => channels.filter((c) => selectedIds.has(c.id)),
    [channels, selectedIds]
  );

  const selectedCreatorIds = useMemo(
    () => [...new Set(selectedChannels.map((c) => c.creatorId))],
    [selectedChannels]
  );

  const handleRefresh = useCallback(() => {
    selectedCreatorIds.forEach((creatorId) =>
      executeCommand({ name: CommandNames.RefreshCreator, creatorId })
    );
  }, [executeCommand, selectedCreatorIds]);

  const handleCheckLive = useCallback(() => {
    selectedCreatorIds.forEach((creatorId) =>
      executeCommand({ name: CommandNames.CheckLiveStreams, creatorId })
    );
  }, [executeCommand, selectedCreatorIds]);

  const handleSave = useCallback(
    async (patch: Partial<Channel>) => {
      setIsSaving(true);
      try {
        await Promise.all(
          selectedChannels.map((ch) =>
            fetchJson<number, Partial<Channel>>({
              path: getQueryPath(`/channel/${ch.id}`),
              method: 'PUT',
              body: { ...ch, ...patch },
              headers: {
                'X-Api-Key': window.Streamarr.apiKey,
                'X-Streamarr-Client': 'Streamarr',
              },
            })
          )
        );
        queryClient.invalidateQueries({ queryKey: ['/channel'] });
        selectedChannels.forEach((ch) => {
          queryClient.invalidateQueries({
            queryKey: [`/channel/creator/${ch.creatorId}`],
          });
        });
        setSelectedIds(new Set());
        setModalOpen(false);
      } finally {
        setIsSaving(false);
      }
    },
    [selectedChannels, queryClient]
  );

  const handleDeleteConfirm = useCallback(async () => {
    setIsDeleting(true);
    try {
      await Promise.all(
        selectedChannels.map((ch) =>
          fetchJson<object, void>({
            path: getQueryPath(`/channel/${ch.id}`),
            method: 'DELETE',
            headers: {
              'X-Api-Key': window.Streamarr.apiKey,
              'X-Streamarr-Client': 'Streamarr',
            },
          })
        )
      );
      queryClient.invalidateQueries({ queryKey: ['/channel'] });
      selectedChannels.forEach((ch) => {
        queryClient.invalidateQueries({
          queryKey: [`/channel/creator/${ch.creatorId}`],
        });
      });
      setSelectedIds(new Set());
      setDeleteConfirmOpen(false);
    } finally {
      setIsDeleting(false);
    }
  }, [selectedChannels, queryClient]);

  const platforms = useMemo(
    () => [...new Set(channels.map((c) => c.platform))].sort(),
    [channels]
  );

  const hasSelection = selectedIds.size > 0;

  return (
    <PageContent title="Channels">
      <PageToolbar>
        <PageToolbarSection>
          <div className={styles.toolbarGroup}>
            <input
              className={styles.searchInput}
              type="text"
              placeholder="Search channels or creators…"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />

            {platforms.length > 1 ? (
              <select
                className={styles.platformFilter}
                value={filterPlatform}
                onChange={(e) => setFilterPlatform(e.target.value)}
              >
                <option value="">All Platforms</option>
                {platforms.map((p) => (
                  <option key={p} value={p}>
                    {PLATFORM_LABELS[p] ?? p}
                  </option>
                ))}
              </select>
            ) : null}
          </div>

          <PageToolbarButton
            label={selectMode ? 'Cancel Select' : 'Select'}
            iconName={selectMode ? icons.CLOSE : icons.CHECK}
            onPress={selectMode ? handleExitSelectMode : () => setSelectMode(true)}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody>
        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && error ? (
          <Alert kind={kinds.DANGER}>Failed to load channels.</Alert>
        ) : null}

        {!isLoading && !error && channels.length === 0 ? (
          <Alert kind={kinds.INFO}>No channels found. Add a creator first.</Alert>
        ) : null}

        {!isLoading && !error && displayed.length > 0 ? (
          <table className={styles.table}>
            <thead>
              <tr>
                {selectMode ? (
                  <th className={styles.checkCell}>
                    <input
                      type="checkbox"
                      checked={allSelected}
                      onChange={handleSelectAll}
                      title={allSelected ? 'Deselect all' : 'Select all'}
                    />
                  </th>
                ) : null}
                <th className={styles.sortableHeader} onClick={() => handleSortPress('creator')}>
                  Creator{sortKey === 'creator' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
                <th className={styles.sortableHeader} onClick={() => handleSortPress('channel')}>
                  Channel{sortKey === 'channel' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
                <th className={styles.sortableHeader} onClick={() => handleSortPress('platform')}>
                  Platform{sortKey === 'platform' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
                <th className={`${styles.centeredCell} ${styles.sortableHeader}`} onClick={() => handleSortPress('monitored')}>
                  Monitored{sortKey === 'monitored' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
                <th className={`${styles.centeredCell} ${styles.sortableHeader}`} onClick={() => handleSortPress('autoDl')}>
                  Auto DL{sortKey === 'autoDl' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
                <th>Watched Words</th>
                <th>Ignored Words</th>
                <th className={`${styles.centeredCell} ${styles.sortableHeader}`} onClick={() => handleSortPress('retention')}>
                  Retention{sortKey === 'retention' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
                </th>
              </tr>
            </thead>
            <tbody>
              {displayed.map((ch) => {
                const creator = creatorsById.get(ch.creatorId);
                return (
                  <tr
                    key={ch.id}
                    className={
                      selectMode && selectedIds.has(ch.id)
                        ? styles.rowSelected
                        : styles.row
                    }
                    onClick={() => handleRowClick(ch)}
                  >
                    {selectMode ? (
                      <td
                        className={styles.checkCell}
                        onClick={(e) => e.stopPropagation()}
                      >
                        <input
                          type="checkbox"
                          checked={selectedIds.has(ch.id)}
                          onChange={() => handleToggle(ch.id)}
                        />
                      </td>
                    ) : null}
                    <td className={styles.cell}>
                      {creator?.title ?? '—'}
                    </td>
                    <td className={styles.cell}>
                      {ch.platformUrl ? (
                        <a
                          className={styles.channelLink}
                          href={ch.platformUrl}
                          target="_blank"
                          rel="noreferrer"
                          onClick={(e) => e.stopPropagation()}
                        >
                          {ch.title}
                        </a>
                      ) : (
                        ch.title
                      )}
                    </td>
                    <td className={styles.cell}>
                      {PLATFORM_LABELS[ch.platform] ?? ch.platform}
                    </td>
                    <td className={styles.centeredCell}>
                      {ch.monitored ? '✓' : '—'}
                    </td>
                    <td className={styles.centeredCell}>
                      {ch.autoDownload ? '✓' : '—'}
                    </td>
                    <td className={`${styles.cell} ${styles.wordCell}`}>
                      {ch.watchedWords || (
                        <span className={styles.empty}>—</span>
                      )}
                    </td>
                    <td className={`${styles.cell} ${styles.wordCell}`}>
                      {ch.ignoredWords || (
                        <span className={styles.empty}>—</span>
                      )}
                    </td>
                    <td className={styles.centeredCell}>
                      {ch.retentionDays != null
                        ? `${ch.retentionDays}d`
                        : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : null}
      </PageContentBody>

      <BulkEditChannelModal
        isOpen={modalOpen}
        selectedChannels={selectedChannels}
        onSave={handleSave}
        isSaving={isSaving}
        onModalClose={() => setModalOpen(false)}
      />

      {selectMode ? (
        <div className={styles.selectBar}>
          <span className={styles.selectBarCount}>
            {selectedIds.size} of {displayed.length} selected
          </span>
          <Button
            kind={kinds.DEFAULT}
            isDisabled={!hasSelection}
            onPress={() => setModalOpen(true)}
          >
            Edit
          </Button>
          <Button
            kind={kinds.DEFAULT}
            isDisabled={!hasSelection}
            onPress={handleRefresh}
          >
            Refresh
          </Button>
          <Button
            kind={kinds.DEFAULT}
            isDisabled={!hasSelection}
            onPress={handleCheckLive}
          >
            Check Live
          </Button>
          <Button
            kind={kinds.DANGER}
            isDisabled={!hasSelection}
            onPress={() => setDeleteConfirmOpen(true)}
          >
            Delete
          </Button>
          <div className={styles.selectBarSpacer} />
          <Button kind={kinds.DEFAULT} onPress={handleExitSelectMode}>
            Cancel
          </Button>
        </div>
      ) : null}

      <ConfirmModal
        isOpen={deleteConfirmOpen}
        kind={kinds.DANGER}
        title="Delete Channels"
        message={`Are you sure you want to delete ${selectedIds.size} channel${selectedIds.size !== 1 ? 's' : ''}? This cannot be undone.`}
        confirmLabel="Delete"
        isSpinning={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteConfirmOpen(false)}
      />
    </PageContent>
  );
}

export default ChannelBulkSettings;
