import React, { useCallback, useState } from 'react';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import NotificationResource from 'typings/Notification';
import AddNotificationModal from './AddNotificationModal';
import EditNotificationModal from './EditNotificationModal';
import { useDeleteNotification, useNotifications } from './useConnectSettings';
import styles from './Connect.css';

function NotificationRow({
  notification,
  onEdit,
}: {
  notification: NotificationResource;
  onEdit: (n: NotificationResource) => void;
}) {
  const { mutate: deleteNotification, isPending: isDeleting } =
    useDeleteNotification(notification.id);

  return (
    <tr>
      <td className={styles.nameCell}>{notification.name}</td>
      <td className={styles.typeCell}>{notification.implementationName}</td>
      <td className={styles.flagCell}>
        {notification.supportsOnDownload ? (
          <Icon
            name={notification.onDownload ? icons.CHECK : icons.SUBTRACT}
            title={
              notification.onDownload
                ? 'On Download: enabled'
                : 'On Download: disabled'
            }
          />
        ) : (
          '—'
        )}
      </td>
      <td className={styles.flagCell}>
        <Icon
          name={notification.enable ? icons.CHECK : icons.SUBTRACT}
          title={notification.enable ? 'Enabled' : 'Disabled'}
        />
      </td>
      <td className={styles.actionsCell}>
        <SpinnerIconButton
          name={icons.EDIT}
          title="Edit"
          isSpinning={false}
          onPress={() => onEdit(notification)}
        />

        <SpinnerIconButton
          name={icons.DELETE}
          title="Delete"
          isSpinning={isDeleting}
          onPress={() => deleteNotification(undefined)}
        />
      </td>
    </tr>
  );
}

function ConnectPage() {
  const { data: notifications, isLoading } = useNotifications();
  const [isAdding, setIsAdding] = useState(false);
  const [editingNotification, setEditingNotification] =
    useState<NotificationResource | null>(null);

  const handleSchemaSelected = useCallback(
    (schema: NotificationResource) => {
      setEditingNotification({
        ...schema,
        id: 0,
        name: '',
        enable: true,
        onDownload: true,
      });
    },
    []
  );

  const handleEdit = useCallback((notification: NotificationResource) => {
    setEditingNotification(notification);
  }, []);

  const handleEditClose = useCallback(() => {
    setEditingNotification(null);
  }, []);

  return (
    <PageContent title="Connect">
      <PageContentBody>
        <div className={styles.header}>
          <h3 className={styles.title}>Notifications</h3>

          <button
            className={styles.addBtn}
            type="button"
            onClick={() => setIsAdding(true)}
          >
            <Icon name={icons.ADD} size={14} />
            Add
          </button>
        </div>

        {isLoading ? (
          <LoadingIndicator />
        ) : notifications && notifications.length > 0 ? (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>On Download</th>
                <th>Enabled</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {notifications.map((n) => (
                <NotificationRow key={n.id} notification={n} onEdit={handleEdit} />
              ))}
            </tbody>
          </table>
        ) : (
          <p className={styles.empty}>
            No notification connections configured. Click Add to get started.
          </p>
        )}
      </PageContentBody>

      <AddNotificationModal
        isOpen={isAdding}
        onSelect={handleSchemaSelected}
        onModalClose={() => setIsAdding(false)}
      />

      {editingNotification && (
        <EditNotificationModal
          notification={editingNotification}
          isOpen={true}
          onModalClose={handleEditClose}
        />
      )}
    </PageContent>
  );
}

export default ConnectPage;
