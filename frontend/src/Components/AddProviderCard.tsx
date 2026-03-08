import React from 'react';
import Button from 'Components/Link/Button';
import Card from 'Components/Card';
import { sizes } from 'Helpers/Props';
import styles from './AddProviderCard.css';

interface AddProviderCardProps {
  implementationName: string;
  infoLink?: string;
  onPress: () => void;
}

function AddProviderCard({
  implementationName,
  infoLink,
  onPress,
}: AddProviderCardProps) {
  return (
    <Card
      className={styles.card}
      overlayContent
      overlayClassName={styles.overlay}
      onPress={onPress}
    >
      <div className={styles.name}>{implementationName}</div>

      {infoLink ? (
        <div className={styles.actions}>
          <Button to={infoLink} size={sizes.SMALL}>
            More Info
          </Button>
        </div>
      ) : null}
    </Card>
  );
}

export default AddProviderCard;
