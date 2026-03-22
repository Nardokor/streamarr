import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { RouteComponentProps, useHistory } from 'react-router-dom';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import CommandNames from 'Commands/CommandNames';
import { useCommandExecuting, useExecuteCommand } from 'Commands/useCommands';
import { icons, kinds } from 'Helpers/Props';
import AddChannelModal from './AddChannelModal';
import CreatorChannelSection from './CreatorChannelSection';
import CreatorUnmatchedSection from './CreatorUnmatchedSection';
import { formatDate } from './creatorUtils';
import {
  useCreatorBySlug,
  useCreatorChannels,
  useCreatorContent,
  useDeleteCreator,
  useReorderChannels,
  useUpdateCreator,
} from './useCreators';
import styles from './CreatorDetail.css';

interface RouteParams {
  slug: string;
}

type Props = RouteComponentProps<RouteParams>;

function CreatorDetail({ match }: Props) {
  const { slug } = match.params;
  const history = useHistory();

  const { data: creator, isLoading: creatorLoading } = useCreatorBySlug(slug);
  const creatorId = creator?.id ?? 0;
  const { data: channels, isLoading: channelsLoading, refetch: refetchChannels } =
    useCreatorChannels(creatorId);
  const { data: content, isLoading: contentLoading } =
    useCreatorContent(creatorId);

  const { deleteCreator, isDeleting } = useDeleteCreator(creatorId);
  const { updateCreator } = useUpdateCreator(creatorId);
  const reorderChannels = useReorderChannels(creatorId);
  const executeCommand = useExecuteCommand();
  const isRefreshing = useCommandExecuting(CommandNames.RefreshCreator);
  const isDownloading = useCommandExecuting(CommandNames.DownloadMissingContent);
  const isRescanning = useCommandExecuting(CommandNames.RescanCreator);
  const isCheckingLive = useCommandExecuting(CommandNames.CheckLiveStreams);

  // When any refresh command finishes, re-fetch channel data so server-side
  // changes (e.g. membershipStatus) are reflected without relying on SignalR.
  const prevRefreshingRef = useRef(false);
  useEffect(() => {
    if (prevRefreshingRef.current && !isRefreshing) {
      refetchChannels();
    }
    prevRefreshingRef.current = isRefreshing;
  }, [isRefreshing, refetchChannels]);

  // Channel ordering — local ordered list derived from server data
  const orderedChannels = useMemo(
    () => [...channels].sort((a, b) => a.sortOrder - b.sortOrder),
    [channels]
  );

  const handleMoveChannel = useCallback(
    (idx: number, direction: -1 | 1) => {
      const swapIdx = idx + direction;
      if (swapIdx < 0 || swapIdx >= orderedChannels.length) return;

      const updated = orderedChannels.map((ch, i) => {
        if (i === idx) return { ...ch, sortOrder: orderedChannels[swapIdx].sortOrder };
        if (i === swapIdx) return { ...ch, sortOrder: orderedChannels[idx].sortOrder };
        return ch;
      });

      // If the two channels happen to have the same sortOrder, assign stable values
      const a = updated[idx];
      const b = updated[swapIdx];
      if (a.sortOrder === b.sortOrder) {
        updated[idx] = { ...a, sortOrder: swapIdx };
        updated[swapIdx] = { ...b, sortOrder: idx };
      }

      reorderChannels([updated[idx], updated[swapIdx]]);
    },
    [orderedChannels, reorderChannels]
  );

  // Custom avatar edit state
  const [avatarEditing, setAvatarEditing] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState('');

  const handleAvatarEditOpen = useCallback(() => {
    setAvatarUrl(creator?.customThumbnailUrl ?? '');
    setAvatarEditing(true);
  }, [creator]);

  const handleAvatarSave = useCallback(() => {
    if (!creator) return;
    // Normalize URLs like "...icon.jpg/revision/latest?cb=..." to "...icon.jpg"
    // by truncating at the end of the first recognized image extension.
    const normalized = avatarUrl.replace(
      /(\.(?:jpg|jpeg|png|gif|webp|svg))(?:\/[^?#]*)?(?:[?#].*)?$/i,
      '$1'
    );
    updateCreator({ ...creator, customThumbnailUrl: normalized }, {
      onSuccess: () => setAvatarEditing(false),
    });
  }, [creator, avatarUrl, updateCreator]);

  const [addChannelOpen, setAddChannelOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const handleRefresh = useCallback(() => {
    executeCommand({ name: CommandNames.RefreshCreator, creatorId });
  }, [executeCommand, creatorId]);

  const handleDownloadMissing = useCallback(() => {
    executeCommand({ name: CommandNames.DownloadMissingContent, creatorId });
  }, [executeCommand, creatorId]);

  const handleRescan = useCallback(() => {
    executeCommand({ name: CommandNames.RescanCreator, creatorId });
  }, [executeCommand, creatorId]);

  const handleCheckLive = useCallback(() => {
    executeCommand({ name: CommandNames.CheckLiveStreams, creatorId });
  }, [executeCommand, creatorId]);

  const handleDeleteConfirm = useCallback(() => {
    deleteCreator(undefined, {
      onSuccess: () => {
        history.push('/creator');
      },
    });
  }, [deleteCreator, history]);

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

  const totalContent = content.length;

  return (
    <PageContent title={creator.title}>
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label="Refresh"
            iconName={icons.REFRESH}
            isSpinning={isRefreshing}
            onPress={handleRefresh}
          />

          <PageToolbarButton
            label="Download Missing"
            iconName={icons.DOWNLOAD}
            isSpinning={isDownloading}
            onPress={handleDownloadMissing}
          />

          <PageToolbarButton
            label="Scan Local Files"
            iconName={icons.SEARCH}
            isSpinning={isRescanning}
            onPress={handleRescan}
          />

          <PageToolbarButton
            label="Check Live"
            iconName={icons.NETWORK}
            isSpinning={isCheckingLive}
            onPress={handleCheckLive}
          />

          <PageToolbarButton
            label="Add Channel"
            iconName={icons.ADD}
            onPress={() => setAddChannelOpen(true)}
          />
        </PageToolbarSection>

        <PageToolbarSection alignContent="right">
          <PageToolbarButton
            label="Delete"
            iconName={icons.DELETE}
            onPress={() => setDeleteConfirmOpen(true)}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody>
        {/* Header */}
        <div className={styles.header}>
          <div className={styles.avatarWrap}>
            {(creator.customThumbnailUrl || creator.thumbnailUrl) ? (
              <img
                className={styles.headerThumbnail}
                src={creator.customThumbnailUrl || creator.thumbnailUrl}
                alt={creator.title}
              />
            ) : null}
            {avatarEditing ? (
              <div className={styles.avatarEdit} onClick={(e) => e.stopPropagation()}>
                <input
                  className={styles.avatarInput}
                  type="url"
                  placeholder="Image URL (blank to clear)"
                  value={avatarUrl}
                  onChange={(e) => setAvatarUrl(e.target.value)}
                  autoFocus
                />
                <button className={styles.avatarSaveBtn} type="button" onClick={handleAvatarSave}>
                  Save
                </button>
                <button className={styles.avatarCancelBtn} type="button" onClick={() => setAvatarEditing(false)}>
                  Cancel
                </button>
              </div>
            ) : (
              <button className={styles.avatarEditBtn} type="button" onClick={handleAvatarEditOpen} title="Set custom avatar">
                <Icon name={icons.EDIT} size={10} />
              </button>
            )}
          </div>

          <div className={styles.headerInfo}>
            <div className={styles.headerTitle}>{creator.title}</div>

            <div className={styles.headerMeta}>
              <span>
                <Icon
                  name={creator.monitored ? icons.MONITORED : icons.UNMONITORED}
                />
                {' '}
                {creator.monitored ? 'Monitored' : 'Unmonitored'}
              </span>
              <span>{creator.path}</span>
              <span>
                {channels.length} channel{channels.length !== 1 ? 's' : ''}
              </span>
              <span>
                {totalContent} item{totalContent !== 1 ? 's' : ''}
              </span>
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

        {/* Per-channel sections, mirroring the season layout */}
        {channels.length === 0 ? (
          <Alert kind={kinds.INFO}>
            No channels associated with this creator yet.
          </Alert>
        ) : null}

        {orderedChannels.map((channel, idx) => (
          <div key={channel.id} className={styles.channelRow}>
            {orderedChannels.length > 1 ? (
              <div className={styles.reorderBtns}>
                <button
                  className={styles.reorderBtn}
                  type="button"
                  disabled={idx === 0}
                  onClick={() => handleMoveChannel(idx, -1)}
                  title="Move up"
                >
                  <Icon name={icons.SORT_ASCENDING} size={10} />
                </button>
                <button
                  className={styles.reorderBtn}
                  type="button"
                  disabled={idx === orderedChannels.length - 1}
                  onClick={() => handleMoveChannel(idx, 1)}
                  title="Move down"
                >
                  <Icon name={icons.SORT_DESCENDING} size={10} />
                </button>
              </div>
            ) : null}
            <div className={styles.channelSectionWrap}>
              <CreatorChannelSection
                channel={channel}
                content={content.filter((c) => c.channelId === channel.id)}
              />
            </div>
          </div>
        ))}

        {channels.length > 0 && totalContent === 0 ? (
          <Alert kind={kinds.INFO}>
            No content synced yet. A sync runs every 60 minutes, or trigger
            one from System &rsaquo; Tasks.
          </Alert>
        ) : null}

        <CreatorUnmatchedSection creatorId={creatorId} channels={channels} />
      </PageContentBody>

      <AddChannelModal
        isOpen={addChannelOpen}
        creatorId={creatorId}
        onModalClose={() => setAddChannelOpen(false)}
      />

      <ConfirmModal
        isOpen={deleteConfirmOpen}
        kind={kinds.DANGER}
        title="Delete Creator"
        message={`Are you sure you want to delete '${creator.title}'? This cannot be undone.`}
        confirmLabel="Delete"
        isSpinning={isDeleting}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteConfirmOpen(false)}
      />
    </PageContent>
  );
}

export default CreatorDetail;
