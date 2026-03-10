import React, { useCallback } from 'react';
import { useHistory } from 'react-router';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Creator from 'typings/Creator';
import { CreatorStats, useUpdateCreator } from './useCreators';
import styles from './CreatorPoster.css';

interface CreatorPosterProps {
  creator: Creator;
  stats?: CreatorStats;
}

function CreatorPoster({ creator, stats }: CreatorPosterProps) {
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

  const downloaded = stats?.downloadedCount ?? 0;
  const wanted = stats?.wantedCount ?? 0;
  const progressPct = wanted > 0 ? Math.round((downloaded / wanted) * 100) : 0;

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
          <img className={styles.thumbnail} src={creator.customThumbnailUrl || thumbnailUrl} alt={title} />
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
        {stats?.isLiveNow ? (
          <div className={styles.liveBadge}>LIVE</div>
        ) : null}
      </div>

      <div className={styles.info}>
        <div className={styles.title}>{title}</div>
        {wanted > 0 ? (
          <div className={styles.progressWrap}>
            <div
              className={styles.progressBar}
              style={{ width: `${progressPct}%` }}
            />
            <span className={styles.progressLabel}>{downloaded} / {wanted}</span>
          </div>
        ) : null}
      </div>
    </div>
  );
}

export default CreatorPoster;
