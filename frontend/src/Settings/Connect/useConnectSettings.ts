import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import NotificationResource from 'typings/Notification';

export const useNotifications = () =>
  useApiQuery<NotificationResource[]>({ path: '/notification' });

export const useNotificationSchema = () =>
  useApiQuery<NotificationResource[]>({ path: '/notification/schema' });

export const useSaveNotification = (onSuccess?: () => void) => {
  const queryClient = useQueryClient();

  return useApiMutation<NotificationResource, NotificationResource>({
    path: '/notification',
    method: 'POST',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['/notification'] });
        onSuccess?.();
      },
    },
  });
};

export const useUpdateNotification = (id: number, onSuccess?: () => void) => {
  const queryClient = useQueryClient();

  return useApiMutation<NotificationResource, NotificationResource>({
    path: `/notification/${id}`,
    method: 'PUT',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['/notification'] });
        onSuccess?.();
      },
    },
  });
};

export const useDeleteNotification = (id: number, onSuccess?: () => void) => {
  const queryClient = useQueryClient();

  return useApiMutation<void, void>({
    path: `/notification/${id}`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['/notification'] });
        onSuccess?.();
      },
    },
  });
};

export const useTestNotification = () => {
  return useApiMutation<void, NotificationResource>({
    path: '/notification/test',
    method: 'POST',
  });
};

export const useTestAllNotifications = () => {
  return useApiMutation<void, void>({
    path: '/notification/testall',
    method: 'POST',
  });
};

export const useToggleNotification = (
  notification: NotificationResource,
  onSuccess?: () => void
) => {
  const queryClient = useQueryClient();

  const { mutate } = useApiMutation<NotificationResource, NotificationResource>(
    {
      path: `/notification/${notification.id}`,
      method: 'PUT',
      mutationOptions: {
        onSuccess: () => {
          queryClient.invalidateQueries({ queryKey: ['/notification'] });
          onSuccess?.();
        },
      },
    }
  );

  return useCallback(() => {
    mutate({ ...notification, enable: !notification.enable });
  }, [mutate, notification]);
};
