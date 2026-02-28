import React, { useCallback, useState } from 'react';
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

  const [notification, setNotification] =
    useState<NotificationResource>(initialNotification);
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(
    null
  );
  const [testMessage, setTestMessage] = useState('');

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
