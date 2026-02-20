import React, { useCallback } from 'react';
import { useHistory } from 'react-router';
import Creator from 'typings/Creator';
import styles from './CreatorPoster.css';

interface CreatorPosterProps {
  creator: Creator;
  channelCount: number;
}

function CreatorPoster({ creator, channelCount }: CreatorPosterProps) {
  const history = useHistory();
  const { id, title, thumbnailUrl, monitored } = creator;

  const handlePress = useCallback(() => {
    history.push(`/creators/${id}`);
  }, [history, id]);

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
      </div>

      <div className={styles.info}>
        <div className={styles.title}>{title}</div>
        <div className={styles.meta}>
          <span
            className={`${styles.monitoredDot} ${monitored ? styles.monitoredDotOn : styles.monitoredDotOff}`}
            title={monitored ? 'Monitored' : 'Unmonitored'}
          />
          <span>{monitored ? 'Monitored' : 'Unmonitored'}</span>
          {channelCount > 0 ? (
            <span>· {channelCount} channel{channelCount !== 1 ? 's' : ''}</span>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export default CreatorPoster;
