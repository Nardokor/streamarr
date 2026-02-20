import React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons, kinds } from 'Helpers/Props';
import CreatorChannelSection from './CreatorChannelSection';
import { formatDate } from './creatorUtils';
import { useCreator, useCreatorChannels, useCreatorContent } from './useCreators';
import styles from './CreatorDetail.css';

interface RouteParams {
  id: string;
}

type Props = RouteComponentProps<RouteParams>;

function CreatorDetail({ match }: Props) {
  const creatorId = parseInt(match.params.id, 10);

  const { data: creator, isLoading: creatorLoading } = useCreator(creatorId);
  const { data: channels, isLoading: channelsLoading } =
    useCreatorChannels(creatorId);
  const { data: content, isLoading: contentLoading } =
    useCreatorContent(creatorId);

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
    </PageContent>
  );
}

export default CreatorDetail;
