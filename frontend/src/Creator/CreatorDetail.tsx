import React, { useCallback, useState } from 'react';
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
import { formatDate } from './creatorUtils';
import {
  useCreator,
  useCreatorChannels,
  useCreatorContent,
  useDeleteCreator,
} from './useCreators';
import styles from './CreatorDetail.css';

interface RouteParams {
  id: string;
}

type Props = RouteComponentProps<RouteParams>;

function CreatorDetail({ match }: Props) {
  const creatorId = parseInt(match.params.id, 10);
  const history = useHistory();

  const { data: creator, isLoading: creatorLoading } = useCreator(creatorId);
  const { data: channels, isLoading: channelsLoading } =
    useCreatorChannels(creatorId);
  const { data: content, isLoading: contentLoading } =
    useCreatorContent(creatorId);

  const { deleteCreator, isDeleting } = useDeleteCreator(creatorId);
  const executeCommand = useExecuteCommand();
  const isRefreshing = useCommandExecuting(CommandNames.RefreshCreator);
  const isDownloading = useCommandExecuting(CommandNames.DownloadMissingContent);

  const [addChannelOpen, setAddChannelOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const handleRefresh = useCallback(() => {
    executeCommand({ name: CommandNames.RefreshCreator, creatorId });
  }, [executeCommand, creatorId]);

  const handleDownloadMissing = useCallback(() => {
    executeCommand({ name: CommandNames.DownloadMissingContent, creatorId });
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

        {channels.map((channel) => (
          <CreatorChannelSection
            key={channel.id}
            channel={channel}
            content={content.filter((c) => c.channelId === channel.id)}
          />
        ))}

        {channels.length > 0 && totalContent === 0 ? (
          <Alert kind={kinds.INFO}>
            No content synced yet. A sync runs every 60 minutes, or trigger
            one from System &rsaquo; Tasks.
          </Alert>
        ) : null}
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
