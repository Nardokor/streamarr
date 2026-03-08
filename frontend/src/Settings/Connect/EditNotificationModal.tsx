import React, { useCallback, useEffect, useRef, useState } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import ProviderFieldFormGroup from 'Components/Form/ProviderFieldFormGroup';
import { inputTypes } from 'Helpers/Props';
import NotificationResource from 'typings/Notification';
import Field from 'typings/Field';
import { InputChanged } from 'typings/inputs';
import {
  useSaveNotification,
  useUpdateNotification,
  useTestNotification,
} from './useConnectSettings';

const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 5 * 60 * 1000;

interface EditNotificationModalProps {
  notification: NotificationResource;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditNotificationModal({
  notification: initialNotification,
  isOpen,
  onModalClose,
}: EditNotificationModalProps) {
  const isNew = initialNotification.id === 0;
  const isPlexServer = initialNotification.implementation === 'PlexServer';

  const [notification, setNotification] =
    useState<NotificationResource>(initialNotification);
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');
  const [isAuthenticating, setIsAuthenticating] = useState(false);
  const [authMessage, setAuthMessage] = useState('');

  const pollTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const pollTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const stopPolling = useCallback(() => {
    if (pollTimerRef.current) {
      clearInterval(pollTimerRef.current);
      pollTimerRef.current = null;
    }
    if (pollTimeoutRef.current) {
      clearTimeout(pollTimeoutRef.current);
      pollTimeoutRef.current = null;
    }
  }, []);

  useEffect(() => stopPolling, [stopPolling]);

  const { mutate: save, isPending: isSaving } = useSaveNotification(
    onModalClose
  );
  const { mutate: update, isPending: isUpdating } = useUpdateNotification(
    notification.id,
    onModalClose
  );
  const { mutate: test, isPending: isTesting } = useTestNotification();

  const handleFieldChange = useCallback(
    (change: InputChanged) => {
      setNotification((prev) => ({
        ...prev,
        fields: prev.fields.map((f) =>
          f.name === change.name ? { ...f, value: change.value as Field['value'] } : f
        ),
      }));
    },
    []
  );

  const handleNameChange = useCallback((change: InputChanged) => {
    setNotification((prev) => ({ ...prev, name: String(change.value) }));
  }, []);

  const handleEnableChange = useCallback((change: InputChanged) => {
    setNotification((prev) => ({ ...prev, enable: Boolean(change.value) }));
  }, []);

  const handleOnDownloadChange = useCallback((change: InputChanged) => {
    setNotification((prev) => ({ ...prev, onDownload: Boolean(change.value) }));
  }, []);

  const handleTest = useCallback(() => {
    test(notification, {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Connection test successful');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(
          (err.statusBody as { message?: string })?.message ??
            err.statusText ??
            'Connection test failed'
        );
      },
    });
  }, [test, notification]);

  const handleSave = useCallback(() => {
    if (isNew) {
      save(notification);
    } else {
      update(notification);
    }
  }, [isNew, save, update, notification]);

  const handlePlexAuth = useCallback(async () => {
    setIsAuthenticating(true);
    setAuthMessage('Opening Plex sign-in…');

    try {
      const startRes = await fetch('/api/v1/notification/action/startOAuth', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(notification),
      });

      if (!startRes.ok) {
        throw new Error('Failed to start Plex OAuth');
      }

      const { oAuthUrl, pinId } = await startRes.json();

      window.open(oAuthUrl, '_blank', 'noopener,noreferrer');
      setAuthMessage('Waiting for Plex sign-in…');

      pollTimerRef.current = setInterval(async () => {
        try {
          const tokenRes = await fetch(
            `/api/v1/notification/action/getOAuthToken?pinId=${pinId}`,
            {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(notification),
            }
          );

          if (!tokenRes.ok) return;

          const { authToken } = await tokenRes.json();

          if (authToken) {
            stopPolling();
            setIsAuthenticating(false);
            setAuthMessage('');
            handleFieldChange({ name: 'authToken', value: authToken });
          }
        } catch {
          // Keep polling
        }
      }, POLL_INTERVAL_MS);

      pollTimeoutRef.current = setTimeout(() => {
        stopPolling();
        setIsAuthenticating(false);
        setAuthMessage('Authentication timed out — please try again.');
      }, POLL_TIMEOUT_MS);
    } catch {
      setIsAuthenticating(false);
      setAuthMessage('Failed to start Plex authentication — check server logs.');
    }
  }, [notification, handleFieldChange, stopPolling]);

  const isBusy = isSaving || isUpdating;

  return (
    <Modal isOpen={isOpen} size="medium" onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {isNew ? `Add ${notification.implementationName}` : notification.name}
        </ModalHeader>

        <ModalBody>
          <FormGroup>
            <FormLabel>Name</FormLabel>
            <FormInputGroup
              type={inputTypes.TEXT}
              name="name"
              value={notification.name}
              onChange={handleNameChange}
            />
          </FormGroup>

          {isPlexServer && (
            <FormGroup>
              <FormLabel>Plex Account</FormLabel>
              <SpinnerButton isSpinning={isAuthenticating} onPress={handlePlexAuth}>
                Authenticate with Plex.tv
              </SpinnerButton>
              {authMessage && (
                <span style={{ marginLeft: 12, fontSize: '0.85em', opacity: 0.8 }}>
                  {authMessage}
                </span>
              )}
            </FormGroup>
          )}

          {notification.fields.map((field: Field) => (
            <ProviderFieldFormGroup
              key={field.name}
              advancedSettings={false}
              {...field}
              pending={false}
              errors={[]}
              warnings={[]}
              onChange={handleFieldChange}
            />
          ))}

          {notification.supportsOnDownload && (
            <FormGroup>
              <FormLabel>On Download</FormLabel>
              <FormInputGroup
                type={inputTypes.CHECK}
                name="onDownload"
                helpText="Send a notification when a video is downloaded"
                value={notification.onDownload}
                onChange={handleOnDownloadChange}
              />
            </FormGroup>
          )}

          <FormGroup>
            <FormLabel>Enable</FormLabel>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="enable"
              value={notification.enable}
              onChange={handleEnableChange}
            />
          </FormGroup>

          {testResult !== null && (
            <Alert kind={testResult === 'success' ? 'success' : 'danger'}>
              {testMessage}
            </Alert>
          )}
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>Cancel</Button>

          <SpinnerButton isSpinning={isTesting} onPress={handleTest}>
            Test
          </SpinnerButton>

          <SpinnerButton isSpinning={isBusy} onPress={handleSave}>
            Save
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default EditNotificationModal;
