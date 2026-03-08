import React, { useCallback, useState } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import { useQualityProfilesData } from 'Settings/Profiles/Quality/useQualityProfiles';
import { InputChanged } from 'typings/inputs';
import { useAddCreator } from './useCreators';
import styles from './AddCreatorModalContent.css';

function sanitize(name: string): string {
  return name.trim().replace(/[\\/:*?"<>|]/g, '');
}

interface Props {
  onModalClose: () => void;
  onCreatorAdded: () => void;
}

function AddCreatorModalContent({ onModalClose, onCreatorAdded }: Props) {
  const [name, setName] = useState('');
  const [rootFolderPath, setRootFolderPath] = useState('');

  const qualityProfiles = useQualityProfilesData();
  const { addCreator, isAdding } = useAddCreator();

  const folderName = sanitize(name);
  const fullPath =
    rootFolderPath && folderName
      ? `${rootFolderPath.replace(/\/+$/, '')}/${folderName}`
      : '';

  const canAdd = !!folderName && !!rootFolderPath && !isAdding;

  const handleNameChange = useCallback(({ value }: InputChanged<string>) => {
    setName(value);
  }, []);

  const handleRootFolderChange = useCallback(
    ({ value }: InputChanged<string>) => {
      setRootFolderPath(value);
    },
    []
  );

  const handleAdd = useCallback(() => {
    if (!canAdd || !fullPath) {
      return;
    }

    addCreator(
      {
        title: name.trim(),
        path: fullPath,
        qualityProfileId: qualityProfiles[0]?.id ?? 1,
        monitored: true,
        channels: [],
        tags: [],
      },
      {
        onSuccess: () => {
          onCreatorAdded();
          onModalClose();
        },
      }
    );
  }, [canAdd, fullPath, name, qualityProfiles, addCreator, onCreatorAdded, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>Add Creator</ModalHeader>

      <ModalBody>
        <div className={styles.container}>
          <div className={styles.info}>
            <Form>
              <FormGroup>
                <FormLabel>Name</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="name"
                  value={name}
                  placeholder="Creator name"
                  autoFocus={true}
                  onChange={handleNameChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>Root Folder</FormLabel>

                <FormInputGroup
                  type={inputTypes.ROOT_FOLDER_SELECT}
                  name="rootFolderPath"
                  value={rootFolderPath}
                  helpText={fullPath || undefined}
                  onChange={handleRootFolderChange}
                />
              </FormGroup>
            </Form>
          </div>
        </div>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>Cancel</Button>

        <SpinnerButton
          kind={kinds.PRIMARY}
          isDisabled={!canAdd}
          isSpinning={isAdding}
          onPress={handleAdd}
        >
          Add Creator
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddCreatorModalContent;
