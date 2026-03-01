import React, { useCallback } from 'react';
import { useHistory } from 'react-router';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Creator from 'typings/Creator';
import { useUpdateCreator } from './useCreators';
import styles from './CreatorPoster.css';

interface CreatorPosterProps {
  creator: Creator;
  channelCount: number;
}

function CreatorPoster({ creator, channelCount }: CreatorPosterProps) {
  const history = useHistory();
  const { id, title, thumbnailUrl, monitored } = creator;
  const { updateCreator, isUpdating } = useUpdateCreator(id);

  const handlePress = useCallback(() => {
    history.push(`/creator/${creator.titleSlug}`);
  }, [history, creator.titleSlug]);

  const handleMonitorToggle = useCallback(
    (value: boolean) => {
      updateCreator({ ...creator, monitored: value });
    },
    [creator, updateCreator]
  );

  const handleMonitorClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
    },
    []
  );

  return (
    <div
      className={styles.card}
      role="link"
      tabIndex={0}
      onClick={handlePress}
      onKeyDown={(e) => e.key === 'Enter' && handlePress()}
    >
      <div className={styles.thumbnailContainer}>
        {thumbnailUrl ? (
          <img className={styles.thumbnail} src={thumbnailUrl} alt={title} />
        ) : (
          <div className={styles.thumbnailPlaceholder}>🎬</div>
        )}
        <div className={styles.monitorToggle} onClick={handleMonitorClick}>
          <MonitorToggleButton
            monitored={monitored}
            isSaving={isUpdating}
            onPress={handleMonitorToggle}
          />
        </div>
      </div>

      <div className={styles.info}>
        <div className={styles.title}>{title}</div>
        <div className={styles.meta}>
          {channelCount > 0 ? (
            <span>{channelCount} channel{channelCount !== 1 ? 's' : ''}</span>
          ) : (
            <span>No channels</span>
          )}
        </div>
      </div>
    </div>
  );
}

export default CreatorPoster;
