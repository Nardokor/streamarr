import React from 'react';
import Card from 'Components/Card';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import styles from './Sources.css';

interface SourceCardProps {
  name: string;
  isConfigured: boolean;
  onPress: () => void;
}

function SourceCard({ name, isConfigured, onPress }: SourceCardProps) {
  return (
    <Card className={styles.sourceCard} onPress={onPress}>
      <div className={styles.cardName}>{name}</div>

      <div
        className={
          isConfigured ? styles.statusConfigured : styles.statusUnconfigured
        }
      >
        <Icon name={icons.CIRCLE} size={8} />

        {isConfigured ? 'Configured' : 'Not configured'}
      </div>
    </Card>
  );
}

export default SourceCard;
