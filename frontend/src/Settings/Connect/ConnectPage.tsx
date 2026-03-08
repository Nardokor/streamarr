import React, { useState } from 'react';
import Card from 'Components/Card';
import Icon from 'Components/Icon';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons } from 'Helpers/Props';
import NotificationResource from 'typings/Notification';
import AddNotificationModal from './AddNotificationModal';
import EditNotificationModal from './EditNotificationModal';
import { useNotifications } from './useConnectSettings';
import styles from './Connect.css';

function NotificationCard({
  notification,
  onPress,
}: {
  notification: NotificationResource;
  onPress: () => void;
}) {
  return (
    <Card className={styles.notificationCard} onPress={onPress}>
      <div className={styles.cardName}>{notification.name}</div>
      <div className={styles.cardType}>{notification.implementationName}</div>
      <div className={notification.enable ? styles.statusEnabled : styles.statusDisabled}>
        <Icon name={icons.CIRCLE} size={8} />
        {notification.enable ? 'Enabled' : 'Disabled'}
      </div>
    </Card>
  );
}

function AddNotificationCard({ onPress }: { onPress: () => void }) {
  return (
    <div className={styles.addCard} onClick={onPress}>
      <Icon name={icons.ADD} size={28} />
    </div>
  );
}

function ConnectPage() {
  const { data: notifications } = useNotifications();
  const [isAdding, setIsAdding] = useState(false);
  const [editingNotification, setEditingNotification] =
    useState<NotificationResource | null>(null);

  return (
    <PageContent title="Connect">
      <PageContentBody>
        <div className={styles.cards}>
          {(notifications ?? []).map((n) => (
            <NotificationCard
              key={n.id}
              notification={n}
              onPress={() => setEditingNotification(n)}
            />
          ))}

          <AddNotificationCard onPress={() => setIsAdding(true)} />
        </div>
      </PageContentBody>

      <AddNotificationModal
        isOpen={isAdding}
        onSelect={(schema) => {
          setEditingNotification({ ...schema, id: 0, name: '', enable: true, onDownload: true });
          setIsAdding(false);
        }}
        onModalClose={() => setIsAdding(false)}
      />

      {editingNotification && (
        <EditNotificationModal
          notification={editingNotification}
          isOpen={true}
          onModalClose={() => setEditingNotification(null)}
        />
      )}
    </PageContent>
  );
}

export default ConnectPage;
