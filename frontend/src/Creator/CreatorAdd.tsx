import React, { useCallback, useState } from 'react';
import { useHistory } from 'react-router';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds } from 'Helpers/Props';
import { useQualityProfilesData } from 'Settings/Profiles/Quality/useQualityProfiles';
import { InputChanged } from 'typings/inputs';
import { useAddCreator } from './useCreators';
import styles from './AddCreatorModalContent.css';

function sanitize(name: string): string {
  return name.trim().replace(/[\\/:*?"<>|]/g, '');
}

function CreatorAdd() {
  const history = useHistory();
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
          history.push('/creator');
        },
      }
    );
  }, [canAdd, fullPath, name, qualityProfiles, addCreator, history]);

  return (
    <PageContent title="Add Creator">
      <PageContentBody>
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

              <FormGroup>
                <SpinnerButton
                  kind={kinds.PRIMARY}
                  isDisabled={!canAdd}
                  isSpinning={isAdding}
                  onPress={handleAdd}
                >
                  Add Creator
                </SpinnerButton>
              </FormGroup>
            </Form>
          </div>
        </div>
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorAdd;
